namespace SecureFix.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureFix.Core.Data;
using SecureFix.Core.Entities;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;
using SecureFix.Core.Services;
using Xunit;

/// <summary>
/// Integration tests for the approval workflow service.
/// Tests the complete approval state machine: workflow status retrieval, approval, and rejection.
/// </summary>
public class ApprovalWorkflowTests : IDisposable
{
    private readonly SecureFixDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApprovalService _approvalService;

    public ApprovalWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<SecureFixDbContext>()
            .UseInMemoryDatabase(databaseName: $"ApprovalWorkflowTests_{Guid.NewGuid()}")
            .Options;

        _context = new SecureFixDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _approvalService = new ApprovalService(_unitOfWork, new MockLogger<ApprovalService>());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test: Get workflow status for a newly ingested alert (no approval decision yet).
    /// Expected: Status should be PendingApproval, approval field null.
    /// </summary>
    [Fact]
    public async Task GetWorkflowStatus_NewAlert_ReturnsPendingApprovalStatus()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var status = await _approvalService.GetWorkflowStatusAsync(alertId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(alertId, status.WorkflowId);
        Assert.Equal(WorkflowStatus.PendingApproval, status.Status);
        Assert.NotNull(status.RiskAssessment);
        Assert.Null(status.Approval);
        Assert.Contains("approval", status.NextStep.ToLower());
    }

    /// <summary>
    /// Test: Approve a pending workflow.
    /// Expected: Approval decision created, workflow status becomes Approved.
    /// </summary>
    [Fact]
    public async Task ApproveAlert_ValidWorkflow_CreatesApprovalAndReturnsApprovedStatus()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";
        var reason = "Package upgrade is safe and tested.";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _approvalService.ApproveAlertAsync(alertId, reviewer, reason);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkflowStatus.Approved, result.Status);
        Assert.NotNull(result.Approval);
        Assert.Equal(ApprovalStatus.Approved, result.Approval.Status);
        Assert.Equal(reviewer, result.Approval.Reviewer);
        Assert.Equal(reason, result.Approval.Reason);
    }

    /// <summary>
    /// Test: Reject a pending workflow.
    /// Expected: Rejection decision created, workflow status becomes Rejected.
    /// </summary>
    [Fact]
    public async Task RejectAlert_ValidWorkflow_CreatesRejectionAndReturnsRejectedStatus()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";
        var reason = "Upgrade requires extensive regression testing.";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 35);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _approvalService.RejectAlertAsync(alertId, reviewer, reason);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkflowStatus.Rejected, result.Status);
        Assert.NotNull(result.Approval);
        Assert.Equal(ApprovalStatus.Rejected, result.Approval.Status);
        Assert.Equal(reviewer, result.Approval.Reviewer);
        Assert.Equal(reason, result.Approval.Reason);
    }

    /// <summary>
    /// Test: Attempt to approve a non-existent workflow.
    /// Expected: InvalidOperationException thrown with "not found" message.
    /// </summary>
    [Fact]
    public async Task ApproveAlert_NonExistentWorkflow_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _approvalService.ApproveAlertAsync(nonExistentId, reviewer)
        );
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Attempt to approve an already-approved workflow.
    /// Expected: InvalidOperationException thrown with "already has a decision" message.
    /// </summary>
    [Fact]
    public async Task ApproveAlert_AlreadyDecided_ThrowsConflictException()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer1 = "reviewer1@example.com";
        var reviewer2 = "reviewer2@example.com";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);
        var existingApproval = CreateTestApprovalDecision(
            alertId, correlationId, reviewer1, ApprovalStatus.Approved
        );

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.ApprovalDecisions.AddAsync(existingApproval);
        await _unitOfWork.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _approvalService.ApproveAlertAsync(alertId, reviewer2)
        );
        Assert.Contains("already has a decision", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: IsApprovedAsync returns true for approved workflows.
    /// Expected: Returns true when approval status is Approved.
    /// </summary>
    [Fact]
    public async Task IsApprovedAsync_ApprovedWorkflow_ReturnsTrue()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        var isApproved = await _approvalService.IsApprovedAsync(alertId);

        // Assert
        Assert.True(isApproved);
    }

    /// <summary>
    /// Test: IsApprovedAsync returns false for rejected workflows.
    /// Expected: Returns false when approval status is Rejected.
    /// </summary>
    [Fact]
    public async Task IsApprovedAsync_RejectedWorkflow_ReturnsFalse()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 35);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.RejectAlertAsync(alertId, reviewer);

        // Act
        var isApproved = await _approvalService.IsApprovedAsync(alertId);

        // Assert
        Assert.False(isApproved);
    }

    /// <summary>
    /// Test: IsApprovedAsync returns false for workflows with no decision yet.
    /// Expected: Returns false when no approval decision exists.
    /// </summary>
    [Fact]
    public async Task IsApprovedAsync_PendingWorkflow_ReturnsFalse()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var isApproved = await _approvalService.IsApprovedAsync(alertId);

        // Assert
        Assert.False(isApproved);
    }

    /// <summary>
    /// Test: Audit events are created for approval decisions.
    /// Expected: One audit event created with EventType "AlertApproved" or "AlertRejected".
    /// </summary>
    [Fact]
    public async Task ApproveAlert_CreatesAuditEvent()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Assert
        var auditEvents = await _unitOfWork.AuditEvents.GetByCorrelationIdAsync(correlationId);
        Assert.NotEmpty(auditEvents);
        var approvalEvent = auditEvents.FirstOrDefault(e => e.EventType == "AlertApproved");
        Assert.NotNull(approvalEvent);
        Assert.Equal(reviewer, approvalEvent.Actor);
        Assert.True(approvalEvent.IsSecurityRelevant);
    }

    /// <summary>
    /// Test: Workflow status response includes complete information for an approved workflow.
    /// Expected: Response includes alert, risk assessment, approval, and recommendation fields (when available).
    /// </summary>
    [Fact]
    public async Task GetWorkflowStatus_ApprovedWorkflow_ReturnsCompleteStatus()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 75);
        var remediation = CreateTestRemediationRecommendation(alertId, correlationId);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.RemediationRecommendations.AddAsync(remediation);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        var status = await _approvalService.GetWorkflowStatusAsync(alertId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(alertId, status.WorkflowId);
        Assert.Equal(WorkflowStatus.Approved, status.Status);
        Assert.NotNull(status.Alert);
        Assert.NotNull(status.RiskAssessment);
        Assert.NotNull(status.Approval);
        Assert.NotNull(status.Remediation);
        Assert.Contains("proceed", status.NextStep.ToLower());
    }

    // Helper methods to create test entities

    private VulnerabilityAlertEntity CreateTestAlert(string alertId, string correlationId)
    {
        return new VulnerabilityAlertEntity
        {
            Id = alertId,
            ExternalAlertId = $"dependabot-{Guid.NewGuid()}",
            CorrelationId = correlationId,
            PackageName = "test-package",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "high",
            CveId = "CVE-2024-12345",
            Description = "Test vulnerability",
            ReceivedAt = DateTimeOffset.UtcNow,
            IsDirectDependency = true,
            IsExploitable = true
        };
    }

    private RiskAssessmentEntity CreateTestRiskAssessment(
        string alertId, string correlationId, int riskScore
    )
    {
        return new RiskAssessmentEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alertId,
            CorrelationId = correlationId,
            RiskScore = riskScore,
            NormalizedSeverity = (int)Severity.High,
            RequiredApprovalLevel = riskScore >= 85 ? "Admin" : "SecurityReviewer",
            ConfidenceScore = 85,
            Summary = "Test risk assessment",
            AssessedAt = DateTimeOffset.UtcNow,
            RiskFactorsJson = @"[""DirectDependency"", ""Exploitable""]"
        };
    }

    private ApprovalDecisionEntity CreateTestApprovalDecision(
        string alertId, string correlationId, string reviewer, ApprovalStatus status
    )
    {
        return new ApprovalDecisionEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alertId,
            WorkflowId = alertId,
            CorrelationId = correlationId,
            Status = (int)status,
            ReviewerIdentity = reviewer,
            ReviewerRole = "SecurityReviewer",
            Reason = "Test decision",
            DecisionTime = DateTimeOffset.UtcNow
        };
    }

    private RemediationRecommendationEntity CreateTestRemediationRecommendation(
        string alertId, string correlationId
    )
    {
        return new RemediationRecommendationEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alertId,
            RiskAssessmentId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            RecommendedAction = "Upgrade",
            TargetVersion = "1.0.1",
            ConfidenceScore = 90,
            ModelIdentifier = "test-model",
            Explanation = "Test explanation",
            PromptVersion = "v1",
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Mock logger for testing (does nothing).
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) { }
}
