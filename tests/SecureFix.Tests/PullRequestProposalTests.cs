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
/// Integration tests for the pull request proposal service.
/// Tests PR proposal generation, validation, and persistence workflows.
/// </summary>
public class PullRequestProposalTests : IDisposable
{
    private readonly SecureFixDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApprovalService _approvalService;
    private readonly IRemediationRecommendationService _remediationService;
    private readonly IPullRequestProposalService _proposalService;

    public PullRequestProposalTests()
    {
        var options = new DbContextOptionsBuilder<SecureFixDbContext>()
            .UseInMemoryDatabase(databaseName: $"PullRequestProposalTests_{Guid.NewGuid()}")
            .Options;

        _context = new SecureFixDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _approvalService = new ApprovalService(_unitOfWork, new MockLogger<ApprovalService>());
        _remediationService = new RemediationRecommendationService(
            _unitOfWork,
            new MockAIRecommendationProvider(),
            _approvalService,
            new MockLogger<RemediationRecommendationService>()
        );
        _proposalService = new PullRequestProposalService(
            _unitOfWork,
            new MockLogger<PullRequestProposalService>()
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Test: Generate proposal for approved vulnerability with high severity.
    /// Expected: Proposal created with title, description, validation commands, rollback guidance.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_ApprovedHighSeverity_CreatesComprehensiveProposal()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        // Mock AI provider will suggest 1.1.0 from installed 1.0.0
        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        // Approve the workflow
        await _approvalService.ApproveAlertAsync(alertId, reviewer);

        // Generate recommendation
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Act
        var proposal = await _proposalService.GenerateProposalAsync(recommendation.Id);

        // Assert
        Assert.NotNull(proposal);
        Assert.Equal(alertId, proposal.AlertId);
        Assert.Equal(correlationId, proposal.CorrelationId);
        Assert.Equal(recommendation.Id, proposal.RecommendationId);
        Assert.NotEmpty(proposal.ProposedTitle);
        Assert.Contains("Security", proposal.ProposedTitle);
        Assert.Contains("lodash", proposal.ProposedTitle);
        Assert.NotEmpty(proposal.ProposedDescription);
        Assert.Contains("High", proposal.ProposedDescription);
        Assert.NotEmpty(proposal.DependencyChanges);
        Assert.NotEmpty(proposal.ValidationCommands);
        Assert.NotEmpty(proposal.RollbackGuidance);
        Assert.NotEmpty(proposal.ResourceLinks);
        Assert.Equal("Minimal", proposal.EstimatedEffort);
        Assert.True(proposal.IsReadyForReview);
        Assert.NotEmpty(proposal.RawProposalJson);
    }

    /// <summary>
    /// Test: Attempt to generate proposal when recommendation doesn't exist.
    /// Expected: InvalidOperationException with "not found" message.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_NonExistentRecommendation_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _proposalService.GenerateProposalAsync(Guid.NewGuid().ToString())
        );
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Attempt to generate proposal when one already exists.
    /// Expected: InvalidOperationException with "already exists" message.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_AlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // First generation succeeds
        var proposal1 = await _proposalService.GenerateProposalAsync(recommendation.Id);
        Assert.NotNull(proposal1);

        // Act & Assert - Second generation should fail
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _proposalService.GenerateProposalAsync(recommendation.Id)
        );
        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test: Get an existing proposal.
    /// Expected: Proposal retrieved with all fields intact.
    /// </summary>
    [Fact]
    public async Task GetProposalAsync_ExistingProposal_ReturnsProposal()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);
        var generatedProposal = await _proposalService.GenerateProposalAsync(recommendation.Id);

        // Act
        var retrieved = await _proposalService.GetProposalAsync(recommendation.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(generatedProposal.Id, retrieved.Id);
        Assert.Equal(generatedProposal.ProposedTitle, retrieved.ProposedTitle);
        Assert.Equal(generatedProposal.ProposedDescription, retrieved.ProposedDescription);
    }

    /// <summary>
    /// Test: Get a non-existent proposal.
    /// Expected: Returns null without throwing.
    /// </summary>
    [Fact]
    public async Task GetProposalAsync_NonExistentProposal_ReturnsNull()
    {
        // Act
        var proposal = await _proposalService.GetProposalAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(proposal);
    }

    /// <summary>
    /// Test: Proposal for medium severity has "Moderate" effort estimate.
    /// Expected: Proposal has correct effort level.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_MediumSeverity_EstimatedEffortModerate()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.Medium);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 65);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Act
        var proposal = await _proposalService.GenerateProposalAsync(recommendation.Id);

        // Assert
        Assert.Equal("Moderate", proposal.EstimatedEffort);
    }

    /// <summary>
    /// Test: Proposal includes audit event.
    /// Expected: AuditEvent created with correct type and actor.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_CreatesAuditEvent()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Act
        await _proposalService.GenerateProposalAsync(recommendation.Id);

        // Assert - Check audit events
        var auditEvents = await _unitOfWork.AuditEvents.GetByCorrelationIdAsync(correlationId);
        var proposalEvent = auditEvents.FirstOrDefault(e => e.EventType == "PullRequestProposalGenerated");
        
        Assert.NotNull(proposalEvent);
        Assert.Equal("Proposal-Service", proposalEvent.Actor);
        Assert.True(proposalEvent.IsSecurityRelevant);
        Assert.Contains("PR proposal", proposalEvent.Summary);
    }

    /// <summary>
    /// Test: Proposal contains common validation commands.
    /// Expected: Multiple language-specific test commands present.
    /// </summary>
    [Fact]
    public async Task GenerateProposalAsync_IncludesValidationCommands()
    {
        // Arrange
        var alertId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var reviewer = "security-reviewer@example.com";

        var alert = CreateTestAlert(alertId, correlationId, "1.0.0", "1.1.0", Severity.High);
        var assessment = CreateTestRiskAssessment(alertId, correlationId, 85);

        await _unitOfWork.VulnerabilityAlerts.AddAsync(alert);
        await _unitOfWork.RiskAssessments.AddAsync(assessment);
        await _unitOfWork.SaveChangesAsync();

        await _approvalService.ApproveAlertAsync(alertId, reviewer);
        var recommendation = await _remediationService.GenerateRecommendationAsync(alertId);

        // Act
        var proposal = await _proposalService.GenerateProposalAsync(recommendation.Id);

        // Assert
        Assert.NotEmpty(proposal.ValidationCommands);
        Assert.Contains(proposal.ValidationCommands, cmd => cmd.Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    private VulnerabilityAlertEntity CreateTestAlert(
        string alertId, string correlationId, string installed, string fixed_, Severity severity)
    {
        return new VulnerabilityAlertEntity
        {
            Id = alertId,
            ExternalAlertId = $"external-{alertId}",
            CorrelationId = correlationId,
            CveId = "CVE-2024-1234",
            PackageName = "lodash",
            InstalledVersion = installed,
            FixedVersion = fixed_,
            ProviderSeverity = severity.ToString(),
            Description = "Test vulnerability in lodash",
            ReceivedAt = DateTimeOffset.UtcNow
        };
    }

    private RiskAssessmentEntity CreateTestRiskAssessment(
        string alertId, string correlationId, int riskScore)
    {
        var severity = riskScore >= 80 ? Severity.High : Severity.Medium;
        return new RiskAssessmentEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alertId,
            CorrelationId = correlationId,
            NormalizedSeverity = (int)severity,
            RiskScore = riskScore,
            RequiredApprovalLevel = "SecurityReviewer",
            Summary = "Test risk assessment",
            RiskFactorsJson = "[]",
            AssessedAt = DateTimeOffset.UtcNow
        };
    }
}
