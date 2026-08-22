namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// AI-generated or fallback remediation recommendation for addressing a vulnerability.
/// This is advisory only and does not authorize any action.
/// All recommendations must pass through human approval.
/// </summary>
public class RemediationRecommendation
{
    /// <summary>
    /// Unique identifier for this recommendation.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Reference to the alert being remediated.
    /// </summary>
    [Required]
    public string AlertId { get; set; } = null!;
    
    /// <summary>
    /// Reference to the risk assessment.
    /// </summary>
    [Required]
    public string RiskAssessmentId { get; set; } = null!;
    
    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;
    
    /// <summary>
    /// Recommended action (e.g., "upgrade_dependency", "patch_applied", "monitor_only").
    /// Must be in an allowed action set.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RecommendedAction { get; set; } = null!;
    
    /// <summary>
    /// Suggested target version. Only populated from trusted input sources.
    /// AI is NOT allowed to invent versions.
    /// </summary>
    [StringLength(100)]
    public string? TargetVersion { get; set; }
    
    /// <summary>
    /// Explanation of the recommendation. Suitable for a human reviewer.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Explanation { get; set; } = null!;
    
    /// <summary>
    /// Key assumptions underlying the recommendation.
    /// </summary>
    [StringLength(1000)]
    public string? Assumptions { get; set; }
    
    /// <summary>
    /// Confidence in this recommendation (0.0 to 1.0).
    /// Used to determine how critical human review is.
    /// </summary>
    [Range(0, 1)]
    public decimal ConfidenceScore { get; set; }
    
    /// <summary>
    /// Identifier of the AI model or provider that generated this recommendation.
    /// Examples: "mock", "azure-openai", "openai", "rules-based-fallback".
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ModelIdentifier { get; set; } = null!;
    
    /// <summary>
    /// Version of the prompt or rules used to generate this recommendation.
    /// Allows audit trail to verify what instruction set was used.
    /// </summary>
    [StringLength(100)]
    public string? PromptVersion { get; set; }
    
    /// <summary>
    /// Whether human review is required before any action.
    /// Always true for this MVP - AI cannot approve actions.
    /// </summary>
    public bool RequiresHumanReview { get; set; } = true;
    
    /// <summary>
    /// Potential risks or side effects of implementing this recommendation.
    /// </summary>
    [StringLength(1000)]
    public string? PotentialRisks { get; set; }
    
    /// <summary>
    /// Alternative recommendations the AI considered but did not select.
    /// Useful for informed decision-making by reviewers.
    /// </summary>
    public List<string> AlternativeActions { get; set; } = new();
    
    /// <summary>
    /// When the recommendation was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
