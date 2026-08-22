namespace SecureFix.Core.Services;

using SecureFix.Core.Models;

/// <summary>
/// Contract for AI-based recommendation providers.
/// Implementations must support:
/// - Azure AI Foundry
/// - Mock provider (for testing)
/// - Rules-based fallback (when AI unavailable)
/// 
/// All implementations MUST include metadata (model, confidence, disclaimer).
/// AI recommendations are ADVISORY ONLY—never automatic approval.
/// </summary>
public interface IAIRecommendationProvider
{
    /// <summary>
    /// Generate a remediation recommendation for a vulnerability.
    /// </summary>
    /// <param name="alert">The vulnerability alert (untrusted input).</param>
    /// <param name="assessment">The risk assessment results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recommendation result with metadata and disclaimer.</returns>
    Task<AIRecommendationResult> RecommendAsync(
        VulnerabilityAlert alert,
        RiskAssessment assessment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the provider identifier (e.g., "azure-ai-foundry", "mock").
    /// </summary>
    string ProviderIdentifier { get; }

    /// <summary>
    /// Check if provider is currently available/healthy.
    /// </summary>
    Task<bool> IsHealthyAsync();
}
