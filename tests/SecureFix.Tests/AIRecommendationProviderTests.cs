namespace SecureFix.Tests;

using SecureFix.Core.Models;
using SecureFix.Core.Services;
using Xunit;

/// <summary>
/// Unit tests for AI recommendation providers.
/// </summary>
public class AIRecommendationProviderTests
{
    private readonly MockAIRecommendationProvider _mockProvider;
    private readonly RulesBasedFallbackAIProvider _rulesProvider;

    public AIRecommendationProviderTests()
    {
        _mockProvider = new MockAIRecommendationProvider();
        _rulesProvider = new RulesBasedFallbackAIProvider();
    }

    [Fact]
    public async Task MockProvider_ReturnsHealthy()
    {
        var isHealthy = await _mockProvider.IsHealthyAsync();
        Assert.True(isHealthy);
    }

    [Fact]
    public async Task MockProvider_ReturnsCriticalRecommendationForCriticalVulnerability()
    {
        var alert = CreateTestAlert("vulnerable-lib", "1.0.0", Severity.Critical.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.Critical,
            RiskScore = 95,
            RiskFactors = ["critical", "exploitable"]
        };

        var result = await _mockProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Upgrade", result.RecommendedAction);
        Assert.NotNull(result.TargetVersion);
        Assert.True(result.ConfidenceScore >= 90);
        Assert.Contains("critical", result.Explanation.ToLower());
    }

    [Fact]
    public async Task MockProvider_ReturnsHighRecommendationForHighSeverity()
    {
        var alert = CreateTestAlert("vulnerable-lib", "1.0.0", Severity.High.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.High,
            RiskScore = 75,
            RiskFactors = ["high", "network-accessible"]
        };

        var result = await _mockProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Upgrade", result.RecommendedAction);
        Assert.NotNull(result.TargetVersion);
    }

    [Fact]
    public async Task MockProvider_ReturnsMediumRecommendationForMediumSeverity()
    {
        var alert = CreateTestAlert("vulnerable-lib", "1.0.0", Severity.Medium.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.Medium,
            RiskScore = 55,
            RiskFactors = ["medium", "requires-auth"]
        };

        var result = await _mockProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Schedule", result.RecommendedAction);
        Assert.NotNull(result.TargetVersion);
    }

    [Fact]
    public async Task MockProvider_ReturnsMonitorRecommendationForLowSeverity()
    {
        var alert = CreateTestAlert("vulnerable-lib", "1.0.0", Severity.Low.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.Low,
            RiskScore = 25,
            RiskFactors = ["low", "informational"]
        };

        var result = await _mockProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Monitor", result.RecommendedAction);
        Assert.Null(result.TargetVersion);
        Assert.Contains("monitor", result.Explanation.ToLower());
    }

    [Fact]
    public async Task MockProvider_IncludesDisclaimer()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.High.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.High,
            RiskScore = 70,
            RiskFactors = []
        };

        var result = await _mockProvider.RecommendAsync(alert, assessment);

        Assert.NotNull(result.Disclaimer);
        Assert.NotEmpty(result.Disclaimer);
    }

    [Fact]
    public async Task RulesProvider_ReturnsCriticalRecommendationForHighScore()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.Critical.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.Critical,
            RiskScore = 90,
            RiskFactors = ["critical"]
        };

        var result = await _rulesProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Upgrade", result.RecommendedAction);
        Assert.NotNull(result.TargetVersion);
    }

    [Fact]
    public async Task RulesProvider_ReturnsScheduleForMediumScore()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.High.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.High,
            RiskScore = 70,
            RiskFactors = ["high"]
        };

        var result = await _rulesProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Schedule", result.RecommendedAction);
    }

    [Fact]
    public async Task RulesProvider_ReturnsMonitorForLowScore()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.Low.ToString());
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.Low,
            RiskScore = 30,
            RiskFactors = ["low"]
        };

        var result = await _rulesProvider.RecommendAsync(alert, assessment);

        Assert.Equal("Monitor", result.RecommendedAction);
        Assert.Null(result.TargetVersion);
    }

    [Fact]
    public async Task RulesProvider_IncludesDirectDependencyNote()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.High.ToString());
        alert.IsDirectDependency = true;
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.High,
            RiskScore = 70,
            RiskFactors = ["high"]
        };

        var result = await _rulesProvider.RecommendAsync(alert, assessment);

        Assert.Contains("Direct dependency", result.Explanation);
    }

    [Fact]
    public async Task RulesProvider_IncludesExploitableNote()
    {
        var alert = CreateTestAlert("lib", "1.0.0", Severity.High.ToString());
        alert.IsExploitable = true;
        var assessment = new RiskAssessment
        {
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            NormalizedSeverity = Severity.High,
            RiskScore = 70,
            RiskFactors = ["high"]
        };

        var result = await _rulesProvider.RecommendAsync(alert, assessment);

        Assert.Contains("Known exploits", result.Explanation);
    }

    [Fact]
    public void MockProvider_HasCorrectIdentifier()
    {
        Assert.Equal("mock-ai-provider", _mockProvider.ProviderIdentifier);
    }

    [Fact]
    public void RulesProvider_HasCorrectIdentifier()
    {
        Assert.Equal("rules-based-fallback", _rulesProvider.ProviderIdentifier);
    }

    private static VulnerabilityAlert CreateTestAlert(string packageName, string version, string severity)
    {
        return new VulnerabilityAlert
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            ExternalAlertId = $"test-{Guid.NewGuid()}",
            PackageName = packageName,
            InstalledVersion = version,
            ProviderSeverity = severity,
            Description = "Test vulnerability"
        };
    }
}
