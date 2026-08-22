namespace SecureFix.Core.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecureFix.Core.Entities;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;

/// <summary>
/// Interface for generating PR proposals from remediation recommendations.
/// </summary>
public interface IPullRequestProposalService
{
    /// <summary>
    /// Generate a draft PR proposal from a remediation recommendation.
    /// </summary>
    Task<PullRequestProposal> GenerateProposalAsync(string recommendationId);

    /// <summary>
    /// Retrieve an existing PR proposal.
    /// </summary>
    Task<PullRequestProposal?> GetProposalAsync(string recommendationId);
}

/// <summary>
/// Service for generating draft pull request proposals.
/// Proposals include title, description, dependency changes, validation commands, and rollback guidance.
/// No actual GitHub PRs are created - this is purely for human review and approval.
/// </summary>
public class PullRequestProposalService : IPullRequestProposalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PullRequestProposalService> _logger;

    public PullRequestProposalService(
        IUnitOfWork unitOfWork,
        ILogger<PullRequestProposalService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate a draft PR proposal from a remediation recommendation.
    /// Gathers context from alert, risk assessment, and recommendation.
    /// Does NOT create an actual GitHub PR.
    /// </summary>
    public async Task<PullRequestProposal> GenerateProposalAsync(string recommendationId)
    {
        ArgumentNullException.ThrowIfNull(recommendationId);

        _logger.LogInformation("Generating PR proposal for recommendation {RecommendationId}", recommendationId);

        // Fetch the recommendation
        var recEntity = await _unitOfWork.RemediationRecommendations.GetByIdAsync(recommendationId);
        if (recEntity == null)
        {
            throw new InvalidOperationException($"Recommendation not found: {recommendationId}");
        }

        // Fetch the alert
        var alert = await _unitOfWork.VulnerabilityAlerts.GetByIdAsync(recEntity.AlertId);
        if (alert == null)
        {
            throw new InvalidOperationException($"Alert not found: {recEntity.AlertId}");
        }

        // Fetch the risk assessment
        var assessment = await _unitOfWork.RiskAssessments.GetByAlertIdAsync(recEntity.AlertId);
        if (assessment == null)
        {
            throw new InvalidOperationException($"Risk assessment not found for alert {recEntity.AlertId}");
        }

        // Check if proposal already exists
        var existing = await _unitOfWork.PullRequestProposals.GetByRecommendationIdAsync(recommendationId);
        if (existing != null)
        {
            _logger.LogWarning("PR proposal already exists for recommendation {RecommendationId}", recommendationId);
            throw new InvalidOperationException(
                $"PR proposal already exists for recommendation {recommendationId}"
            );
        }

        // Build proposal
        var proposal = BuildProposal(alert, assessment, recEntity);

        // Map to entity and persist
        var entity = MapDomainToEntity(proposal, assessment.Id);
        await _unitOfWork.PullRequestProposals.AddAsync(entity);

        // Create audit event
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = alert.CorrelationId,
            EventType = "PullRequestProposalGenerated",
            Summary = $"PR proposal generated for {alert.PackageName}",
            Details = $"Proposal: {proposal.ProposedTitle}",
            IsSecurityRelevant = true,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "Proposal-Service",
            Level = "Info"
        };

        await _unitOfWork.AuditEvents.AddAsync(MapAuditDomainToEntity(auditEvent));
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "PR proposal generated and persisted for recommendation {RecommendationId}",
            recommendationId
        );

        return proposal;
    }

    /// <summary>
    /// Retrieve an existing PR proposal by recommendation ID.
    /// </summary>
    public async Task<PullRequestProposal?> GetProposalAsync(string recommendationId)
    {
        ArgumentNullException.ThrowIfNull(recommendationId);

        var entity = await _unitOfWork.PullRequestProposals.GetByRecommendationIdAsync(recommendationId);
        if (entity == null)
        {
            return null;
        }

        return MapEntityToDomain(entity);
    }

    /// <summary>
    /// Build a comprehensive PR proposal from alert, assessment, and recommendation.
    /// </summary>
    private static PullRequestProposal BuildProposal(
        VulnerabilityAlertEntity alert,
        RiskAssessmentEntity assessment,
        RemediationRecommendationEntity recommendation)
    {
        var proposal = new PullRequestProposal
        {
            Id = Guid.NewGuid().ToString(),
            RecommendationId = recommendation.Id,
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            ProposedTitle = GenerateTitle(alert, recommendation),
            ProposedDescription = GenerateDescription(alert, assessment, recommendation),
            FilesForReview = new List<string>
            {
                "package.json",
                "package-lock.json",
                ".csproj files",
                "requirements.txt",
                "pyproject.toml"
            }.Where(f => IsRelevantToRepo(f, alert)).ToList(),
            DependencyChanges = new List<string>
            {
                $"Upgrade {alert.PackageName} from {alert.InstalledVersion} to {recommendation.TargetVersion}"
            },
            ValidationCommands = GenerateValidationCommands(),
            RollbackGuidance = GenerateRollbackGuidance(alert, recommendation),
            KnownLimitations = GenerateKnownLimitations(recommendation),
            ResourceLinks = new List<string>
            {
                $"https://www.cvedetails.com/cve/{alert.CveId}/",
                alert.AdvisoryUrl ?? string.Empty
            }.Where(l => !string.IsNullOrEmpty(l)).ToList(),
            EstimatedEffort = DetermineEffort(recommendation),
            IsReadyForReview = true,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        // Store raw JSON for archival
        proposal.RawProposalJson = JsonSerializer.Serialize(proposal, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return proposal;
    }

    private static string GenerateTitle(VulnerabilityAlertEntity alert, RemediationRecommendationEntity recommendation)
    {
        return recommendation.RecommendedAction switch
        {
            "Upgrade" => $"Security: Upgrade {alert.PackageName} to {recommendation.TargetVersion}",
            "Schedule" => $"Security: Schedule upgrade of {alert.PackageName}",
            _ => $"Security: Address {alert.CveId} in {alert.PackageName}"
        };
    }

    private static string GenerateDescription(
        VulnerabilityAlertEntity alert,
        RiskAssessmentEntity assessment,
        RemediationRecommendationEntity recommendation)
    {
        var severity = assessment.NormalizedSeverity switch
        {
            4 => "Critical",
            3 => "High",
            2 => "Medium",
            _ => "Low"
        };

        return $@"## Security Update: {alert.PackageName}

### Vulnerability
- **CVE**: {alert.CveId}
- **Package**: {alert.PackageName}
- **Current Version**: {alert.InstalledVersion}
- **Fixed Version**: {alert.FixedVersion}
- **Severity**: {severity}
- **Description**: {alert.Description}

### Recommendation
**Action**: {recommendation.RecommendedAction}
**Confidence**: {recommendation.ConfidenceScore:P0}

{recommendation.Explanation}

### Risk Assessment
- **Risk Score**: {assessment.RiskScore}
- **Summary**: {assessment.Summary}

### Proposed Change
Upgrade {alert.PackageName} from {alert.InstalledVersion} to {recommendation.TargetVersion}

### Validation
Before merging:
- Run full test suite
- Check dependency compatibility
- Review release notes for breaking changes

### Rollback Plan
If issues occur after merge:
1. Revert the dependency upgrade
2. Restart affected services
3. Monitor for stability

---
*This proposal was AI-generated and requires human review and approval before creating a PR.*";
    }

    private static List<string> GenerateValidationCommands()
    {
        return new List<string>
        {
            "npm test",
            "dotnet test",
            "python -m pytest",
            "go test ./...",
            "cargo test"
        };
    }

    private static string GenerateRollbackGuidance(VulnerabilityAlertEntity alert, RemediationRecommendationEntity recommendation)
    {
        return $@"If the upgrade causes issues:
1. Revert the package.json/package-lock.json or .csproj change
2. Run: npm install / dotnet restore
3. Restart the application
4. Monitor logs for errors
5. If issues persist, file an issue with: package name, version, error details";
    }

    private static string GenerateKnownLimitations(RemediationRecommendationEntity recommendation)
    {
        return $@"- Confidence: {recommendation.ConfidenceScore:P0}
- AI-generated proposal; human judgment required
- Breaking changes in target version not fully analyzed
- Compatibility with other dependencies requires testing";
    }

    private static string DetermineEffort(RemediationRecommendationEntity recommendation)
    {
        return recommendation.RecommendedAction switch
        {
            "Upgrade" => "Minimal",
            "Schedule" => "Moderate",
            _ => "High"
        };
    }

    private static bool IsRelevantToRepo(string file, VulnerabilityAlertEntity alert)
    {
        // In MVP, include all - production could detect repo language
        return true;
    }

    /// <summary>
    /// Map domain model to entity for persistence.
    /// </summary>
    private static PullRequestProposalEntity MapDomainToEntity(PullRequestProposal proposal, string riskAssessmentId)
    {
        return new PullRequestProposalEntity
        {
            Id = proposal.Id,
            RecommendationId = proposal.RecommendationId,
            AlertId = proposal.AlertId,
            CorrelationId = proposal.CorrelationId,
            ProposedTitle = proposal.ProposedTitle,
            ProposedDescription = proposal.ProposedDescription,
            FilesForReviewJson = JsonSerializer.Serialize(proposal.FilesForReview),
            DependencyChangesJson = JsonSerializer.Serialize(proposal.DependencyChanges),
            ValidationCommandsJson = JsonSerializer.Serialize(proposal.ValidationCommands),
            RollbackGuidance = proposal.RollbackGuidance,
            KnownLimitations = proposal.KnownLimitations,
            ResourceLinksJson = JsonSerializer.Serialize(proposal.ResourceLinks),
            EstimatedEffort = proposal.EstimatedEffort,
            IsReadyForReview = proposal.IsReadyForReview,
            RawProposalJson = proposal.RawProposalJson,
            GeneratedAt = proposal.GeneratedAt
        };
    }

    /// <summary>
    /// Map entity to domain model for API responses.
    /// </summary>
    private static PullRequestProposal MapEntityToDomain(PullRequestProposalEntity entity)
    {
        return new PullRequestProposal
        {
            Id = entity.Id,
            RecommendationId = entity.RecommendationId,
            AlertId = entity.AlertId,
            CorrelationId = entity.CorrelationId,
            ProposedTitle = entity.ProposedTitle,
            ProposedDescription = entity.ProposedDescription,
            FilesForReview = JsonSerializer.Deserialize<List<string>>(entity.FilesForReviewJson) ?? new(),
            DependencyChanges = JsonSerializer.Deserialize<List<string>>(entity.DependencyChangesJson) ?? new(),
            ValidationCommands = JsonSerializer.Deserialize<List<string>>(entity.ValidationCommandsJson) ?? new(),
            RollbackGuidance = entity.RollbackGuidance,
            KnownLimitations = entity.KnownLimitations,
            ResourceLinks = JsonSerializer.Deserialize<List<string>>(entity.ResourceLinksJson) ?? new(),
            EstimatedEffort = entity.EstimatedEffort,
            IsReadyForReview = entity.IsReadyForReview,
            RawProposalJson = entity.RawProposalJson,
            GeneratedAt = entity.GeneratedAt
        };
    }

    /// <summary>
    /// Map audit domain model to entity.
    /// </summary>
    private static AuditEventEntity MapAuditDomainToEntity(AuditEvent auditEvent)
    {
        return new AuditEventEntity
        {
            Id = auditEvent.Id,
            CorrelationId = auditEvent.CorrelationId,
            EventType = auditEvent.EventType,
            Level = auditEvent.Level,
            Summary = auditEvent.Summary,
            Details = auditEvent.Details,
            IsSecurityRelevant = auditEvent.IsSecurityRelevant,
            Timestamp = auditEvent.Timestamp,
            Actor = auditEvent.Actor,
            Service = "PullRequestProposalService"
        };
    }
}
