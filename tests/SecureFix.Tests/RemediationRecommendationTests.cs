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
/// Integration tests for the remediation recommendation service.
/// Tests AI recommendation generation, validation, and persistence workflows.
/// </summary>
public class RemediationRecommendationTests : IDisposable
{
    private readonly SecureFixDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MockAIRecommendationProvider _aiProvider;
    private readonly IApprovalService _approvalService;
    private readonly IRemediationRecommendationService _remediationService;

    public RemediationRecommendationTests()
    {
        var options = new DbContextOptionsBuilder<SecureFixDbContext>()
            .UseInMemoryDatabase(databaseName: $"RemediationRecommendationTests_{Guid.NewGuid()}")
            .Options;

        _context = new SecureFixDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _aiProvider = new MockAIRecommendationProvider();
        _approvalService = new ApprovalService(_unitOfWork, new MockLogger<ApprovalService>());
        _remediationService = new RemediationRecommendationService(
            _unitOfWork,
            _aiProvider,
            _approvalService,
            new MockLogger<RemediationRecommendationService>()
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test: Generate recommendation for approved workflow with high severity.
    /// Expected: Recommendation created with correct action, version, and confidence.
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_ApprovedHighSeverity_CreatesUpgradeRecommendation()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Approve the workflow
        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Assert
        Assert.NotNull(recommendation);
        Assert.Equal(alertId, recommendation.AlertId);
        Assert.Equal(correlationId, recommendation.CorrelationId);
        Assert.Equal("Upgrade", recommendation.RecommendedAction);
        // Mock provider suggests 1.1.0 (next safe minor version from 1.0.0)
        Assert.Equal("1.1.0", recommendation.TargetVersion);
        Assert.True(recommendation.ConfidenceScore > 0);
        Assert.NotEmpty(recommendation.Explanation);
        Assert.NotEmpty(recommendation.ModelIdentifier);
    }

    /// <summary>
    /// Test: Attempt to generate recommendation for unapproved workflow.
    /// Expected: InvalidOperationException with "not approved" message.
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_NotApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.Medium);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 50);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _remediationService.GenerateRecommendationAsync(alertId)
        );
        Assert.Contains("not approved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Generate recommendation for low severity vulnerability.
    /// Expected: Recommendation with "Monitor" action instead of "Upgrade".
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_LowSeverity_CreatesMonitorRecommendation()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.Low);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 25);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Assert
        Assert.NotNull(recommendation);
        Assert.Equal("Monitor", recommendation.RecommendedAction);
    }

    /// <summary>
    /// Test: Attempt to generate recommendation for non-approved workflow.
    /// Expected: InvalidOperationException with "not approved" message.
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_NonApprovedWorkflow_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _remediationService.GenerateRecommendationAsync(Guid.NewGuid().ToString())
        );
        Assert.Contains("not approved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Attempt to generate recommendation when one already exists.
    /// Expected: InvalidOperationException with "already exists" message.
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_AlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 80);
        var existingRecommendation = CreateTestRemediationRecommendation(alertId, assessment.Id, correlationId);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.RemediationRecommendations.AddAsync(existingRecommendation);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _remediationService.GenerateRecommendationAsync(alertId)
        );
        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Get an existing recommendation.
    /// Expected: Returns the recommendation with all metadata.
    /// </summary>
    [Fact]
    public async Task GetRecommendationAsync_ExistingRecommendation_ReturnsRecommendation()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var generated = await _remediationService.GenerateRecommendationAsync(alertId);

        // Act
        var retrieved = await _remediationService.GetRecommendationAsync(alertId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(generated.Id, retrieved.Id);
        Assert.Equal(generated.RecommendedAction, retrieved.RecommendedAction);
        Assert.Equal(generated.ConfidenceScore, retrieved.ConfidenceScore);
    }

    /// <summary>
    /// Test: Get recommendation for workflow with no recommendation.
    /// Expected: Returns null.
    /// </summary>
    [Fact]
    public async Task GetRecommendationAsync_NoRecommendation_ReturnsNull()
    {
        // Act
        var retrieved = await _remediationService.GetRecommendationAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(retrieved);
    }

    /// <summary>
    /// Test: Recommendation creation creates audit event.
    /// Expected: One audit event with EventType "RemediationRecommendationGenerated".
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_CreatesAuditEvent()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        await _remediationService.GenerateRecommendationAsync(alertId);

        // Assert
        var auditEvents = await _unitOfWork.AuditEvents.GetByCorrelationIdAsync(correlationId);
        var recEvent = auditEvents.FirstOrDefault(e => e.EventType == "RemediationRecommendationGenerated");
        Assert.NotNull(recEvent);
        Assert.True(recEvent.IsSecurityRelevant);
        Assert.Contains("Upgrade", recEvent.Details);
    }

    /// <summary>
    /// Test: Recommendation validates AI confidence score.
    /// Expected: Confidence score is between 0 and 1.
    /// </summary>
    [Fact]
    public async Task GenerateRecommendationAsync_ConfidenceScoreValid()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.0.1", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Act
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Assert
        Assert.True(recommendation.ConfidenceScore >= 0);
        Assert.True(recommendation.ConfidenceScore <= 1);
    }

    // Helper methods

    private VulnerabilityAlertEntity CreateTestAlert(
        string alertId, string correlationId, string installedVersion, string fixedVersion, Severity severity
    )
    {
        return new VulnerabilityAlertEntity
        {
            Id = alertId,
            ExternalAlertId = $"dependabot-{Guid.NewGuid()}",
            CorrelationId = correlationId,
            PackageName = "test-package",
            InstalledVersion = installedVersion,
            FixedVersion = fixedVersion,
            ProviderSeverity = severity.ToString().ToLower(),
            CveId = "CVE-2024-12345",
            Description = "Test vulnerability for remediation",
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
            NormalizedSeverity = (int)(riskScore >= 80 ? Severity.Critical : riskScore >= 60 ? Severity.High : riskScore >= 40 ? Severity.Medium : Severity.Low),
            RequiredApprovalLevel = riskScore >= 85 ? "Admin" : "SecurityReviewer",
            ConfidenceScore = 85,
            Summary = "Test risk assessment",
            AssessedAt = DateTimeOffset.UtcNow,
            RiskFactorsJson = @"[""DirectDependency"", ""Exploitable""]"
        };
    }

    private RemediationRecommendationEntity CreateTestRemediationRecommendation(
        string alertId, string riskAssessmentId, string correlationId
    )
    {
        return new RemediationRecommendationEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alertId,
            RiskAssessmentId = riskAssessmentId,
            CorrelationId = correlationId,
            RecommendedAction = "Upgrade",
            TargetVersion = "1.0.1",
            ConfidenceScore = 0.90m,
            ModelIdentifier = "test-model",
            Explanation = "Test explanation",
            PromptVersion = "v1",
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
