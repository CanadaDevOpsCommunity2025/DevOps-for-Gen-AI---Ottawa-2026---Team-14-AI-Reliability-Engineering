namespace SecureFix.Core.Services;

using Microsoft.Extensions.Logging;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;


/// <summary>
/// Contract for approval workflow service.
/// Manages approval decisions and enforces workflow gates.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Get workflow status including alert, assessment, and approval state.
    /// </summary>
    Task<WorkflowStatusResponse?> GetWorkflowStatusAsync(string workflowId);

    /// <summary>
    /// Approve an alert for remediation.
    /// </summary>
    Task<WorkflowStatusResponse> ApproveAlertAsync(string workflowId, string reviewer, string? reason = null);

    /// <summary>
    /// Reject an alert, blocking further remediation.
    /// </summary>
    Task<WorkflowStatusResponse> RejectAlertAsync(string workflowId, string reviewer, string? reason = null);

    /// <summary>
    /// Check if alert is approved (required before remediation).
    /// </summary>
    Task<bool> IsApprovedAsync(string workflowId);
}

/// <summary>
/// Approval service implementation.
/// Orchestrates workflow state transitions and persistence.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IUnitOfWork unitOfWork,
        ILogger<ApprovalService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkflowStatusResponse?> GetWorkflowStatusAsync(string workflowId)
    {
        ArgumentNullException.ThrowIfNull(workflowId);

        // Fetch alert
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(workflowId);
        if (alert == null)
        {
            _logger.LogWarning("Workflow not found: {WorkflowId}", workflowId);
            return null;
        }

        // Fetch risk assessment
        var assessment = await _unitOfWork.RiskAssessments.GetByAlertIdAsync(workflowId);

        // Fetch approval decision
        var approval = await _unitOfWork.ApprovalDecisions.GetByAlertIdAsync(workflowId);

        // Fetch remediation recommendation
        var remediation = await _unitOfWork.RemediationRecommendations.GetByAlertIdAsync(workflowId);

        // Determine workflow status
        WorkflowStatus status = WorkflowStatus.Received;
        if (approval != null)
        {
            status = (ApprovalStatus)approval.Status == ApprovalStatus.Approved 
                ? WorkflowStatus.Approved 
                : WorkflowStatus.Rejected;
        }
        else if (assessment != null)
        {
            status = WorkflowStatus.PendingApproval;
        }

        return new WorkflowStatusResponse
        {
            WorkflowId = workflowId,
            CorrelationId = alert.CorrelationId,
            Status = status,
            Alert = MapAlertToSummary(alert),
            RiskAssessment = assessment != null ? MapAssessmentToSummary(assessment) : null,
            Approval = approval != null ? MapApprovalToSummary(approval) : null,
            Remediation = remediation != null ? MapRemediationToSummary(remediation) : null,
            NextStep = DetermineNextStep(status, approval),
            CreatedAt = alert.ReceivedAt,
            UpdatedAt = approval?.DecisionTime ?? assessment?.AssessedAt ?? alert.ReceivedAt
        };
    }

    public async Task<WorkflowStatusResponse> ApproveAlertAsync(string workflowId, string reviewer, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(workflowId);
        ArgumentNullException.ThrowIfNull(reviewer);

        _logger.LogInformation("Approving workflow {WorkflowId} by {Reviewer}", workflowId, reviewer);

        // Fetch alert to verify it exists
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(workflowId);
        if (alert == null)
        {
            throw new InvalidOperationException($"Workflow not found: {workflowId}");
        }

        // Check if already has a decision
        var existingDecision = await _unitOfWork.ApprovalDecisions.GetByAlertIdAsync(workflowId);
        if (existingDecision != null)
        {
            throw new InvalidOperationException($"Workflow already has a decision: {existingDecision.Status}");
        }

        // Create approval decision
        var decision = new ApprovalDecision
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = workflowId,
            WorkflowId = workflowId,
            CorrelationId = alert.CorrelationId,
            Status = ApprovalStatus.Approved,
            ReviewerIdentity = reviewer,
            ReviewerRole = "SecurityReviewer",
            Reason = reason ?? "Approved by SecurityReviewer",
            DecisionTime = DateTimeOffset.UtcNow
        };

        // Create audit event
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = alert.CorrelationId,
            EventType = "AlertApproved",
            Summary = "Alert approved for remediation",
            Details = $"Approved by {reviewer}. Reason: {reason ?? "N/A"}",
            IsSecurityRelevant = true,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = reviewer,
            Level = "Warning"
        };

        // Persist
        await _unitOfWork.ApprovalDecisions.AddAsync(MapDomainToApprovalEntity(decision));
        await _unitOfWork.AuditEvents.AddAsync(MapDomainToAuditEntity(auditEvent));
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Workflow {WorkflowId} approved by {Reviewer}", workflowId, reviewer);

        // Return updated workflow status
        var status = await GetWorkflowStatusAsync(workflowId);
        return status ?? throw new InvalidOperationException("Failed to retrieve workflow after approval");
    }

    public async Task<WorkflowStatusResponse> RejectAlertAsync(string workflowId, string reviewer, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(workflowId);
        ArgumentNullException.ThrowIfNull(reviewer);

        _logger.LogInformation("Rejecting workflow {WorkflowId} by {Reviewer}", workflowId, reviewer);

        // Fetch alert to verify it exists
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(workflowId);
        if (alert == null)
        {
            throw new InvalidOperationException($"Workflow not found: {workflowId}");
        }

        // Check if already has a decision
        var existingDecision = await _unitOfWork.ApprovalDecisions.GetByAlertIdAsync(workflowId);
        if (existingDecision != null)
        {
            throw new InvalidOperationException($"Workflow already has a decision: {existingDecision.Status}");
        }

        // Create rejection decision
        var decision = new ApprovalDecision
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = workflowId,
            WorkflowId = workflowId,
            CorrelationId = alert.CorrelationId,
            Status = ApprovalStatus.Rejected,
            ReviewerIdentity = reviewer,
            ReviewerRole = "SecurityReviewer",
            Reason = reason ?? "Rejected by SecurityReviewer",
            DecisionTime = DateTimeOffset.UtcNow
        };

        // Create audit event
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = alert.CorrelationId,
            EventType = "AlertRejected",
            Summary = "Alert rejected - remediation blocked",
            Details = $"Rejected by {reviewer}. Reason: {reason ?? "N/A"}",
            IsSecurityRelevant = true,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = reviewer,
            Level = "Error"
        };

        // Persist
        await _unitOfWork.ApprovalDecisions.AddAsync(MapDomainToApprovalEntity(decision));
        await _unitOfWork.AuditEvents.AddAsync(MapDomainToAuditEntity(auditEvent));
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Workflow {WorkflowId} rejected by {Reviewer}", workflowId, reviewer);

        // Return updated workflow status
        var status = await GetWorkflowStatusAsync(workflowId);
        return status ?? throw new InvalidOperationException("Failed to retrieve workflow after rejection");
    }

    public async Task<bool> IsApprovedAsync(string workflowId)
    {
        var approval = await _unitOfWork.ApprovalDecisions.GetByAlertIdAsync(workflowId);
        return approval != null && (ApprovalStatus)approval.Status == ApprovalStatus.Approved;
    }

    private static AlertSummary MapAlertToSummary(Entities.VulnerabilityAlertEntity alert)
    {
        return new AlertSummary
        {
            Id = alert.Id,
            PackageName = alert.PackageName,
            InstalledVersion = alert.InstalledVersion,
            FixedVersion = alert.FixedVersion,
            ProviderSeverity = alert.ProviderSeverity,
            CveId = alert.CveId,
            Description = alert.Description
        };
    }

    private static RiskAssessmentSummary MapAssessmentToSummary(Entities.RiskAssessmentEntity assessment)
    {
        var riskFactors = new List<string>();
        if (!string.IsNullOrEmpty(assessment.RiskFactorsJson))
        {
            try
            {
                riskFactors = System.Text.Json.JsonSerializer.Deserialize<List<string>>(assessment.RiskFactorsJson) ?? [];
            }
            catch
            {
                riskFactors = [];
            }
        }

        return new RiskAssessmentSummary
        {
            RiskScore = assessment.RiskScore,
            NormalizedSeverity = (Severity)assessment.NormalizedSeverity,
            RequiredApprovalLevel = assessment.RequiredApprovalLevel,
            RiskFactors = riskFactors
        };
    }

    private static ApprovalSummary MapApprovalToSummary(Entities.ApprovalDecisionEntity approval)
    {
        return new ApprovalSummary
        {
            Status = (ApprovalStatus)approval.Status,
            Reviewer = approval.ReviewerIdentity,
            Reason = approval.Reason,
            DecidedAt = approval.DecisionTime
        };
    }

    private static RemediationSummary MapRemediationToSummary(Entities.RemediationRecommendationEntity remediation)
    {
        return new RemediationSummary
        {
            RecommendedAction = remediation.RecommendedAction,
            TargetVersion = remediation.TargetVersion,
            ConfidenceScore = remediation.ConfidenceScore,
            ModelIdentifier = remediation.ModelIdentifier
        };
    }

    private static string DetermineNextStep(WorkflowStatus status, Entities.ApprovalDecisionEntity? approval)
    {
        return status switch
        {
            WorkflowStatus.Received => "Awaiting risk assessment",
            WorkflowStatus.PendingApproval => "Awaiting SecurityReviewer approval",
            WorkflowStatus.Approved => "Remediation approved - proceed to PR generation",
            WorkflowStatus.Rejected => "Remediation rejected - no further action",
            _ => "Unknown status"
        };
    }

    private static Entities.ApprovalDecisionEntity MapDomainToApprovalEntity(ApprovalDecision domain)
    {
        return new Entities.ApprovalDecisionEntity
        {
            Id = domain.Id,
            WorkflowId = domain.WorkflowId,
            AlertId = domain.AlertId,
            CorrelationId = domain.CorrelationId,
            Status = (int)domain.Status,
            ReviewerIdentity = domain.ReviewerIdentity,
            ReviewerRole = domain.ReviewerRole,
            Reason = domain.Reason,
            Comments = domain.Comments,
            DecisionTime = domain.DecisionTime,
            ClientIdentifier = domain.ClientIdentifier
        };
    }

    private static Entities.AuditEventEntity MapDomainToAuditEntity(AuditEvent domain)
    {
        return new Entities.AuditEventEntity
        {
            Id = domain.Id,
            CorrelationId = domain.CorrelationId,
            EventType = domain.EventType,
            Summary = domain.Summary,
            Details = domain.Details,
            IsSecurityRelevant = domain.IsSecurityRelevant,
            Timestamp = domain.Timestamp,
            Actor = domain.Actor,
            Level = domain.Level
        };
    }
}
