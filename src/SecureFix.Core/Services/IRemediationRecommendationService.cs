namespace SecureFix.Core.Services;

using Microsoft.Extensions.Logging;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;

/// <summary>
/// Service for generating remediation recommendations via AI provider.
/// Responsible for calling the AI recommendation provider, validating responses,
/// and persisting recommendations for audit and approval tracking.
/// </summary>
public interface IRemediationRecommendationService
{
    /// <summary>
    /// Generate a remediation recommendation for an approved workflow.
    /// Only succeeds if the workflow is already approved.
    /// </summary>
    /// <param name="workflowId">The workflow/alert ID.</param>
    /// <returns>Persisted remediation recommendation with all metadata.</returns>
    /// <exception cref="InvalidOperationException">Workflow not found, not approved, or already has recommendation.</exception>
    Task<RemediationRecommendation> GenerateRecommendationAsync(string workflowId);

    /// <summary>
    /// Get an existing remediation recommendation for a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow/alert ID.</param>
    /// <returns>The recommendation if it exists, null otherwise.</returns>
    Task<RemediationRecommendation?> GetRecommendationAsync(string workflowId);
}

/// <summary>
/// Implementation of remediation recommendation generation service.
/// </summary>
public class RemediationRecommendationService : IRemediationRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIRecommendationProvider _aiProvider;
    private readonly IApprovalService _approvalService;
    private readonly ILogger<RemediationRecommendationService> _logger;

    public RemediationRecommendationService(
        IUnitOfWork unitOfWork,
        IAIRecommendationProvider aiProvider,
        IApprovalService approvalService,
        ILogger<RemediationRecommendationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate a remediation recommendation for an approved workflow.
    /// Workflow must be approved before recommendation can be generated.
    /// </summary>
    public async Task<RemediationRecommendation> GenerateRecommendationAsync(string workflowId)
    {
        ArgumentNullException.ThrowIfNull(workflowId);

        _logger.LogInformation("Generating remediation recommendation for workflow {WorkflowId}", workflowId);

        // Verify workflow exists and is approved
        var isApproved = await _approvalService.IsApprovedAsync(workflowId);
        if (!isApproved)
        {
            throw new InvalidOperationException(
                $"Workflow {workflowId} is not approved. Remediation recommendations require approval."
            );
        }

        // Fetch the alert and risk assessment to provide context to AI
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(workflowId);
        if (alert == null)
        {
            throw new InvalidOperationException($"Workflow not found: {workflowId}");
        }

        var assessment = await _unitOfWork.RiskAssessments.GetByAlertIdAsync(workflowId);
        if (assessment == null)
        {
            throw new InvalidOperationException($"Risk assessment not found for workflow {workflowId}");
        }

        // Check if recommendation already exists
        var existing = await _unitOfWork.RemediationRecommendations.GetByAlertIdAsync(workflowId);
        if (existing != null)
        {
            _logger.LogWarning("Remediation recommendation already exists for workflow {WorkflowId}", workflowId);
            throw new InvalidOperationException(
                $"Remediation recommendation already exists for workflow {workflowId}"
            );
        }

        // Call AI provider to get recommendation
        _logger.LogInformation(
            "Calling AI provider for recommendation: package={Package}, severity={Severity}",
            alert.PackageName,
            alert.ProviderSeverity
        );

        var aiResult = await _aiProvider.RecommendAsync(
            alert: MapAlertEntityToDomain(alert),
            assessment: MapAssessmentEntityToDomain(assessment)
        );

        if (aiResult == null)
        {
            throw new InvalidOperationException("AI provider returned null recommendation");
        }

        // Validate AI response - reject if version doesn't match expected
        ValidateAIRecommendation(aiResult, alert);

        // Create remediation recommendation domain model
        var recommendation = new RemediationRecommendation
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = workflowId,
            CorrelationId = alert.CorrelationId,
            RecommendedAction = aiResult.RecommendedAction,
            TargetVersion = aiResult.TargetVersion ?? alert.FixedVersion,
            Explanation = aiResult.Explanation,
            Assumptions = null,  // Not provided by AI provider
            ConfidenceScore = aiResult.ConfidenceScore / 100m,  // Convert 0-100 to 0-1
            ModelIdentifier = aiResult.ModelIdentifier,
            PromptVersion = aiResult.PromptVersion,
            RequiresHumanReview = true,  // All recommendations require review by default
            GeneratedAt = DateTimeOffset.UtcNow
        };

        // Persist recommendation
        var entity = MapDomainToEntity(recommendation, assessment.Id);
        await _unitOfWork.RemediationRecommendations.AddAsync(entity);

        // Create audit event
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = alert.CorrelationId,
            EventType = "RemediationRecommendationGenerated",
            Summary = $"AI recommendation generated for {alert.PackageName}",
            Details = $"Action: {recommendation.RecommendedAction}, Target: {recommendation.TargetVersion}, Confidence: {recommendation.ConfidenceScore:P0}, Model: {recommendation.ModelIdentifier}",
            IsSecurityRelevant = true,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "AI-Provider",
            Level = "Info"
        };

        await _unitOfWork.AuditEvents.AddAsync(MapAuditDomainToEntity(auditEvent));
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Remediation recommendation generated for workflow {WorkflowId}: action={Action}, target={TargetVersion}, confidence={Confidence}",
            workflowId,
            recommendation.RecommendedAction,
            recommendation.TargetVersion,
            recommendation.ConfidenceScore
        );

        return recommendation;
    }

    /// <summary>
    /// Get an existing remediation recommendation for a workflow.
    /// </summary>
    public async Task<RemediationRecommendation?> GetRecommendationAsync(string workflowId)
    {
        ArgumentNullException.ThrowIfNull(workflowId);

        var entity = await _unitOfWork.RemediationRecommendations.GetByAlertIdAsync(workflowId);
        if (entity == null)
        {
            return null;
        }

        return new RemediationRecommendation
        {
            Id = entity.Id,
            AlertId = entity.AlertId,
            CorrelationId = entity.CorrelationId,
            RecommendedAction = entity.RecommendedAction,
            TargetVersion = entity.TargetVersion,
            Explanation = entity.Explanation,
            Assumptions = entity.Assumptions,
            ConfidenceScore = entity.ConfidenceScore,
            ModelIdentifier = entity.ModelIdentifier,
            PromptVersion = entity.PromptVersion,
            RequiresHumanReview = entity.RequiresHumanReview,
            GeneratedAt = entity.GeneratedAt
        };
    }

    /// <summary>
    /// Validate AI recommendation output.
    /// Rejects recommendations that suggest versions not in the trusted input.
    /// </summary>
    private static void ValidateAIRecommendation(AIRecommendationResult aiResult, Entities.VulnerabilityAlertEntity alert)
    {
        // If AI suggested a target version, verify it's the fixed version from the alert
         if (!string.IsNullOrEmpty(aiResult.TargetVersion))
         {
             // Allow the AI to suggest the fixed version or a higher version (e.g., latest)
             // But reject if it invents a version that doesn't match any trusted source
             // For MVP, we only trust the fixed version from the vulnerability alert
             if (!string.Equals(aiResult.TargetVersion, alert.FixedVersion, StringComparison.Ordinal))
             {
                 // Log warning but allow if it's clearly a later version (basic semver check)
                 if (!string.IsNullOrEmpty(alert.FixedVersion) && !IsLaterVersion(aiResult.TargetVersion, alert.FixedVersion))
                 {
                     throw new InvalidOperationException(
                         $"AI suggested untrusted version {aiResult.TargetVersion}. " +
                         $"Only versions from the alert ({alert.FixedVersion}) are trusted for this MVP."
                     );
                 }
             }
         }

        // Validate confidence score is in valid range (0-100)
        if (aiResult.ConfidenceScore < 0 || aiResult.ConfidenceScore > 100)
        {
            throw new InvalidOperationException(
                $"Invalid confidence score {aiResult.ConfidenceScore}. Must be between 0 and 100."
            );
        }

        // Validate required fields are not empty
        if (string.IsNullOrWhiteSpace(aiResult.RecommendedAction))
        {
            throw new InvalidOperationException("AI recommendation missing RecommendedAction");
        }

        if (string.IsNullOrWhiteSpace(aiResult.Explanation))
        {
            throw new InvalidOperationException("AI recommendation missing Explanation");
        }

        if (string.IsNullOrWhiteSpace(aiResult.ModelIdentifier))
        {
            throw new InvalidOperationException("AI recommendation missing ModelIdentifier");
        }
    }

    /// <summary>
    /// Basic semver comparison to detect if suggested version is newer.
    /// Returns true if suggestedVersion appears to be later than baseVersion.
    /// </summary>
    private static bool IsLaterVersion(string suggestedVersion, string baseVersion)
    {
        try
        {
            var suggested = System.Text.RegularExpressions.Regex.Match(suggestedVersion, @"(\d+)\.(\d+)\.(\d+)");
            var baseMatch = System.Text.RegularExpressions.Regex.Match(baseVersion, @"(\d+)\.(\d+)\.(\d+)");

            if (!suggested.Success || !baseMatch.Success)
            {
                return false;
            }

            int sugMajor = int.Parse(suggested.Groups[1].Value);
            int sugMinor = int.Parse(suggested.Groups[2].Value);
            int sugPatch = int.Parse(suggested.Groups[3].Value);

            int baseMajor = int.Parse(baseMatch.Groups[1].Value);
            int baseMinor = int.Parse(baseMatch.Groups[2].Value);
            int basePatch = int.Parse(baseMatch.Groups[3].Value);

            if (sugMajor > baseMajor) return true;
            if (sugMajor < baseMajor) return false;
            if (sugMinor > baseMinor) return true;
            if (sugMinor < baseMinor) return false;
            return sugPatch > basePatch;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Map domain model to entity for persistence.
    /// </summary>
    private static Entities.RemediationRecommendationEntity MapDomainToEntity(
        RemediationRecommendation domain,
        string riskAssessmentId
    )
    {
        return new Entities.RemediationRecommendationEntity
        {
            Id = domain.Id,
            AlertId = domain.AlertId,
            RiskAssessmentId = riskAssessmentId,
            CorrelationId = domain.CorrelationId,
            RecommendedAction = domain.RecommendedAction,
            TargetVersion = domain.TargetVersion,
            Explanation = domain.Explanation,
            Assumptions = domain.Assumptions,
            ConfidenceScore = domain.ConfidenceScore,
            ModelIdentifier = domain.ModelIdentifier,
            PromptVersion = domain.PromptVersion,
            RequiresHumanReview = domain.RequiresHumanReview,
            GeneratedAt = domain.GeneratedAt
        };
    }

    /// <summary>
    /// Map audit domain model to entity for persistence.
    /// </summary>
    private static Entities.AuditEventEntity MapAuditDomainToEntity(AuditEvent domain)
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

    /// <summary>
    /// Map alert entity to domain model for AI provider.
    /// </summary>
    private static VulnerabilityAlert MapAlertEntityToDomain(Entities.VulnerabilityAlertEntity entity)
    {
        return new VulnerabilityAlert
        {
            Id = entity.Id,
            ExternalAlertId = entity.ExternalAlertId,
            CorrelationId = entity.CorrelationId,
            PackageName = entity.PackageName,
            InstalledVersion = entity.InstalledVersion,
            FixedVersion = entity.FixedVersion,
            ProviderSeverity = entity.ProviderSeverity,
            CveId = entity.CveId,
            Description = entity.Description,
            ReceivedAt = entity.ReceivedAt,
            IsDirectDependency = entity.IsDirectDependency,
            IsExploitable = entity.IsExploitable
        };
    }

    /// <summary>
    /// Map risk assessment entity to domain model for AI provider.
    /// </summary>
    private static RiskAssessment MapAssessmentEntityToDomain(Entities.RiskAssessmentEntity entity)
    {
        return new RiskAssessment
        {
            Id = entity.Id,
            AlertId = entity.AlertId,
            CorrelationId = entity.CorrelationId,
            RiskScore = entity.RiskScore,
            NormalizedSeverity = (Severity)entity.NormalizedSeverity,
            RequiredApprovalLevel = entity.RequiredApprovalLevel,
            ConfidenceScore = entity.ConfidenceScore,
            Summary = entity.Summary,
            AssessedAt = entity.AssessedAt
        };
    }
}
