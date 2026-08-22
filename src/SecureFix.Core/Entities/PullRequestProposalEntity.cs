namespace SecureFix.Core.Entities;

/// <summary>
/// Entity Framework entity for PullRequestProposal persistence.
/// </summary>
public class PullRequestProposalEntity
{
    public string Id { get; set; } = null!;
    public string RecommendationId { get; set; } = null!;
    public string AlertId { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public string ProposedTitle { get; set; } = null!;
    public string ProposedDescription { get; set; } = null!;
    public string FilesForReviewJson { get; set; } = "[]";
    public string DependencyChangesJson { get; set; } = "[]";
    public string ValidationCommandsJson { get; set; } = "[]";
    public string? RollbackGuidance { get; set; }
    public string? KnownLimitations { get; set; }
    public string ResourceLinksJson { get; set; } = "[]";
    public string? EstimatedEffort { get; set; }
    public bool IsReadyForReview { get; set; }
    public string? RawProposalJson { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    // Navigation properties
    public RemediationRecommendationEntity? Recommendation { get; set; }
    public VulnerabilityAlertEntity? Alert { get; set; }
}
