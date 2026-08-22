namespace SecureFix.Core.Services;

using Microsoft.Extensions.Logging;
using SecureFix.Core.Models;

/// <summary>
/// Provider factory that manages AI provider selection and fallback strategy.
/// Configuration determines primary provider; automatically falls back on failures.
/// </summary>
public interface IAIRecommendationProviderFactory
{
    /// <summary>
    /// Get the primary recommendation provider.
    /// </summary>
    IAIRecommendationProvider GetProvider();

    /// <summary>
    /// Get a provider by name (for testing or explicit override).
    /// </summary>
    IAIRecommendationProvider GetProvider(string providerName);
}

/// <summary>
/// Default implementation of provider factory.
/// Supports: "azure-ai-foundry", "mock", "rules-based-fallback".
/// </summary>
public class AIRecommendationProviderFactory : IAIRecommendationProviderFactory
{
    private readonly ILogger<AIRecommendationProviderFactory> _logger;
    private readonly ILogger<AzureAIFoundryRecommendationProvider> _azureLogger;
    private readonly string _primaryProvider;
    private readonly MockAIRecommendationProvider _mockProvider;
    private readonly RulesBasedFallbackAIProvider _fallbackProvider;
    private AzureAIFoundryRecommendationProvider? _azureProvider;

    public AIRecommendationProviderFactory(
        ILogger<AIRecommendationProviderFactory> logger,
        ILogger<AzureAIFoundryRecommendationProvider> azureLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureLogger = azureLogger ?? throw new ArgumentNullException(nameof(azureLogger));
        _mockProvider = new MockAIRecommendationProvider();
        _fallbackProvider = new RulesBasedFallbackAIProvider();

        // Determine primary provider from environment
        _primaryProvider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "mock";

        _logger.LogInformation("AI recommendation provider factory initialized: primary={Primary}",
            _primaryProvider);
    }

    public IAIRecommendationProvider GetProvider()
    {
        return GetProvider(_primaryProvider);
    }

    public IAIRecommendationProvider GetProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "azure-ai-foundry" => GetAzureProvider(),
            "mock" => _mockProvider,
            "rules-based-fallback" => _fallbackProvider,
            _ => throw new InvalidOperationException($"Unknown provider: {providerName}")
        };
    }

    private AzureAIFoundryRecommendationProvider GetAzureProvider()
    {
        // Lazy initialization—only create if needed
        _azureProvider ??= new AzureAIFoundryRecommendationProvider(_azureLogger);
        return _azureProvider;
    }
}
