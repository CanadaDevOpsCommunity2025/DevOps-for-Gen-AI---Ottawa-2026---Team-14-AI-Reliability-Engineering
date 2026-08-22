namespace SecureFix.Tests;

using SecureFix.Core.Models;
using SecureFix.Core.Services;
using Xunit;

public class RiskScoringEngineTests
{
    private readonly RiskScoringPolicy _policy;
    private readonly RiskScoringEngine _engine;

    public RiskScoringEngineTests()
    {
        _policy = new RiskScoringPolicy();
        _engine = new RiskScoringEngine(_policy);
    }

    [Fact]
    public async Task AssessRiskAsync_WithCriticalSeverity_ProducesCriticalRating()
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "alert-1",
            CorrelationId = "corr-1",
            ExternalAlertId = "ext-1",
            PackageName = "vulnerable-lib",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "critical",
            IsDirectDependency = true,
            IsExploitable = true
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alert-1", result.AlertId);
        Assert.Equal("corr-1", result.CorrelationId);
        Assert.Equal(Severity.Critical, result.NormalizedSeverity);
        Assert.True(result.RiskScore >= 90, $"Expected score >= 90, got {result.RiskScore}");
        Assert.Equal(1.0m, result.ConfidenceScore); // Deterministic = full confidence
    }

    [Fact]
    public async Task AssessRiskAsync_WithHighSeverity_ProducesHighRating()
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "alert-2",
            CorrelationId = "corr-2",
            ExternalAlertId = "ext-2",
            PackageName = "medium-risk-lib",
            InstalledVersion = "2.0.0",
            FixedVersion = "2.0.1",
            ProviderSeverity = "high",
            IsDirectDependency = true,
            IsExploitable = false
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Severity.High, result.NormalizedSeverity);
        Assert.InRange(result.RiskScore, 70, 89);
    }

    [Fact]
    public async Task AssessRiskAsync_WithTransitiveDependency_ReducesScore()
    {
        // Arrange
        var directAlert = new VulnerabilityAlert
        {
            Id = "direct-1",
            CorrelationId = "corr-d1",
            ExternalAlertId = "ext-d1",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "high",
            IsDirectDependency = true
        };

        var transitiveAlert = new VulnerabilityAlert
        {
            Id = "transitive-1",
            CorrelationId = "corr-t1",
            ExternalAlertId = "ext-t1",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "high",
            IsDirectDependency = false // Transitive
        };

        // Act
        var directResult = await _engine.AssessRiskAsync(directAlert);
        var transitiveResult = await _engine.AssessRiskAsync(transitiveAlert);

        // Assert
        Assert.True(directResult.RiskScore > transitiveResult.RiskScore,
            $"Direct ({directResult.RiskScore}) should score higher than transitive ({transitiveResult.RiskScore})");
    }

    [Fact]
    public async Task AssessRiskAsync_WithExploitableFlag_IncreasesScore()
    {
        // Arrange
        var nonExploitableAlert = new VulnerabilityAlert
        {
            Id = "non-exp-1",
            CorrelationId = "corr-ne",
            ExternalAlertId = "ext-ne",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "medium",
            IsExploitable = false
        };

        var exploitableAlert = new VulnerabilityAlert
        {
            Id = "exp-1",
            CorrelationId = "corr-e",
            ExternalAlertId = "ext-e",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "medium",
            IsExploitable = true
        };

        // Act
        var nonExpResult = await _engine.AssessRiskAsync(nonExploitableAlert);
        var expResult = await _engine.AssessRiskAsync(exploitableAlert);

        // Assert
        Assert.True(expResult.RiskScore > nonExpResult.RiskScore,
            $"Exploitable ({expResult.RiskScore}) should score higher than non-exploitable ({nonExpResult.RiskScore})");
        Assert.Contains("Known exploitation bonus", expResult.RiskFactors.FirstOrDefault(f => f.Contains("exploitation")) ?? "");
    }

    [Fact]
    public async Task AssessRiskAsync_WithoutFixedVersion_IncreasesScore()
    {
        // Arrange
        var withFixAlert = new VulnerabilityAlert
        {
            Id = "with-fix-1",
            CorrelationId = "corr-wf",
            ExternalAlertId = "ext-wf",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "high"
        };

        var withoutFixAlert = new VulnerabilityAlert
        {
            Id = "without-fix-1",
            CorrelationId = "corr-nf",
            ExternalAlertId = "ext-nf",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            FixedVersion = null,
            ProviderSeverity = "high"
        };

        // Act
        var withFixResult = await _engine.AssessRiskAsync(withFixAlert);
        var withoutFixResult = await _engine.AssessRiskAsync(withoutFixAlert);

        // Assert
        Assert.True(withoutFixResult.RiskScore > withFixResult.RiskScore,
            $"No-fix ({withoutFixResult.RiskScore}) should score higher than with-fix ({withFixResult.RiskScore})");
    }

    [Fact]
    public async Task AssessRiskAsync_ScoreClampedTo100()
    {
        // Arrange - create maximum score scenario
        var alert = new VulnerabilityAlert
        {
            Id = "max-1",
            CorrelationId = "corr-max",
            ExternalAlertId = "ext-max",
            PackageName = "critical-lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "critical",
            IsDirectDependency = true,
            IsExploitable = true
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.InRange(result.RiskScore, 0, 100);
    }

    [Fact]
    public async Task AssessRiskAsync_ScoreClampedToZero()
    {
        // Arrange
        var policy = new RiskScoringPolicy
        {
            SeverityBaseScores = new() { { "unknown", 0 } }
        };
        var engine = new RiskScoringEngine(policy);

        var alert = new VulnerabilityAlert
        {
            Id = "min-1",
            CorrelationId = "corr-min",
            ExternalAlertId = "ext-min",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "unknown",
            IsDirectDependency = false
        };

        // Act
        var result = await engine.AssessRiskAsync(alert);

        // Assert
        Assert.InRange(result.RiskScore, 0, 100);
    }

    [Theory]
    [InlineData("critical", Severity.Critical)]
    [InlineData("high", Severity.High)]
    [InlineData("medium", Severity.Medium)]
    [InlineData("low", Severity.Low)]
    [InlineData("info", Severity.Medium)]
    [InlineData("unknown", Severity.Medium)]
    public async Task AssessRiskAsync_NormalizesProviderSeverity(string providerSeverity, Severity expected)
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "norm-1",
            CorrelationId = "corr-norm",
            ExternalAlertId = "ext-norm",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = providerSeverity
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.Equal(expected, result.NormalizedSeverity);
    }

    [Fact]
    public async Task AssessRiskAsync_ProducesExplainableFactors()
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "factors-1",
            CorrelationId = "corr-factors",
            ExternalAlertId = "ext-factors",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "high",
            IsDirectDependency = true,
            IsExploitable = true
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.NotEmpty(result.RiskFactors);
        Assert.Contains("Base score", result.RiskFactors.FirstOrDefault(f => f.Contains("Base")) ?? "");
        Assert.Contains("Direct dependency", result.RiskFactors.FirstOrDefault(f => f.Contains("Direct")) ?? "");
        Assert.Contains("Known exploitation", result.RiskFactors.FirstOrDefault(f => f.Contains("exploitation")) ?? "");
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public async Task AssessRiskAsync_HighScoreDeterminesAdminApproval()
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "admin-1",
            CorrelationId = "corr-admin",
            ExternalAlertId = "ext-admin",
            PackageName = "critical-lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "critical",
            IsDirectDependency = true,
            IsExploitable = true
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.Equal("Admin", result.RequiredApprovalLevel);
    }

    [Fact]
    public async Task AssessRiskAsync_LowScoreStillRequiresSecurityReviewerApproval()
    {
        // Arrange
        var alert = new VulnerabilityAlert
        {
            Id = "low-1",
            CorrelationId = "corr-low",
            ExternalAlertId = "ext-low",
            PackageName = "lib",
            InstalledVersion = "1.0.0",
            ProviderSeverity = "low",
            IsDirectDependency = false
        };

        // Act
        var result = await _engine.AssessRiskAsync(alert);

        // Assert
        Assert.Equal("SecurityReviewer", result.RequiredApprovalLevel);
    }

    [Fact]
    public async Task AssessRiskAsync_NullAlertThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.AssessRiskAsync(null!));
    }
}
