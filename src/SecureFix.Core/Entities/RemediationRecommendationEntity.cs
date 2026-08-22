namespace SecureFix.Core.Entities;

/// <summary>
/// Entity Framework entity for RemediationRecommendation persistence.
/// </summary>
public class RemediationRecommendationEntity
{
    public string Id { get; set; } = null!;
    public string AlertId { get; set; } = null!;
    public string RiskAssessmentId { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public string RecommendedAction { get; set; } = null!;
    public string? TargetVersion { get; set; }
    public string Explanation { get; set; } = null!;
    public string? Assumptions { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ModelIdentifier { get; set; } = null!;
    public string? PromptVersion { get; set; }
    public bool RequiresHumanReview { get; set; }
    public string? PotentialRisks { get; set; }
    public string AlternativeActionsJson { get; set; } = "[]";
    public DateTimeOffset GeneratedAt { get; set; }

    // Navigation properties
    public VulnerabilityAlertEntity? Alert { get; set; }
}
