namespace SecureFix.Core.Models;

/// <summary>
/// Result from an AI recommendation provider.
/// All AI responses must include metadata for traceability.
/// </summary>
public class AIRecommendationResult
{
    /// <summary>
    /// Recommended remediation action (e.g., "Upgrade" or "Patch" or "Monitor").
    /// </summary>
    public required string RecommendedAction { get; set; }

    /// <summary>
    /// Specific package or version to upgrade to.
    /// </summary>
    public string? TargetVersion { get; set; }

    /// <summary>
    /// Explanation of why this recommendation was made.
    /// May be truncated if AI response was lengthy.
    /// </summary>
    public required string Explanation { get; set; }

    /// <summary>
    /// Confidence score (0-100) of the recommendation.
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Model identifier used (e.g., "gpt-4", "mock-provider").
    /// </summary>
    public required string ModelIdentifier { get; set; }

    /// <summary>
    /// Disclaimer (e.g., "AI recommendation is advisory only").
    /// </summary>
    public required string Disclaimer { get; set; }

    /// <summary>
    /// Prompt version or hash used for this recommendation.
    /// </summary>
    public string? PromptVersion { get; set; }

    /// <summary>
    /// Raw risk factors extracted from assessment.
    /// </summary>
    public required List<string> RiskFactors { get; set; } = [];

    /// <summary>
    /// Alternative remediation actions (if any).
    /// </summary>
    public required List<string> AlternativeActions { get; set; } = [];
}
