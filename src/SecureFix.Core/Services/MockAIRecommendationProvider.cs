namespace SecureFix.Core.Services;

using SecureFix.Core.Models;

/// <summary>
/// Mock AI provider for testing and local development.
/// Returns deterministic recommendations without calling external AI.
/// </summary>
public class MockAIRecommendationProvider : IAIRecommendationProvider
{
    public string ProviderIdentifier => "mock-ai-provider";

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }

    public Task<AIRecommendationResult> RecommendAsync(
        VulnerabilityAlert alert,
        RiskAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        // Deterministic logic based on package and severity
        string recommendedAction;
        string? targetVersion;
        string explanation;
        int confidenceScore;
        var alternativeActions = new List<string>();

        if (assessment.NormalizedSeverity >= Severity.Critical)
        {
            recommendedAction = "Upgrade";
            targetVersion = DetermineNextSafeVersion(alert.InstalledVersion);
            explanation =
                $"Critical severity vulnerability in {alert.PackageName}. " +
                $"Immediate upgrade to {targetVersion} is strongly recommended. " +
                $"No workarounds available for this vulnerability type.";
            confidenceScore = 95;
            alternativeActions.AddRange(new[]
            {
                "Apply vendor-provided security patch if available",
                "Isolate affected service pending upgrade"
            });
        }
        else if (assessment.NormalizedSeverity >= Severity.High)
        {
            recommendedAction = "Upgrade";
            targetVersion = DetermineNextSafeVersion(alert.InstalledVersion);
            explanation =
                $"High severity vulnerability detected. " +
                $"Upgrade {alert.PackageName} to {targetVersion} within the next release cycle.";
            confidenceScore = 85;
            alternativeActions.AddRange(new[]
            {
                "Monitor usage patterns in production",
                "Apply vendor guidance if patch available"
            });
        }
        else if (assessment.NormalizedSeverity >= Severity.Medium)
        {
            recommendedAction = "Schedule";
            targetVersion = DetermineNextSafeVersion(alert.InstalledVersion);
            explanation =
                $"Medium severity vulnerability. Schedule upgrade in next planned release. " +
                $"Ensure automated tests cover vulnerability scenario.";
            confidenceScore = 70;
            alternativeActions.AddRange(new[]
            {
                "Monitor security advisories for patches",
                "Implement input validation workarounds"
            });
        }
        else
        {
            recommendedAction = "Monitor";
            targetVersion = null;
            explanation =
                $"Low severity vulnerability. Continue monitoring but no immediate action required. " +
                $"Consider upgrade in next major version cycle.";
            confidenceScore = 60;
        }

        var result = new AIRecommendationResult
        {
            ModelIdentifier = ProviderIdentifier,
            RecommendedAction = recommendedAction,
            TargetVersion = targetVersion,
            Explanation = explanation,
            Disclaimer = "Mock recommendation for testing only. This is not real AI output.",
            PromptVersion = "mock-1.0",
            ConfidenceScore = confidenceScore,
            RiskFactors = assessment.RiskFactors.ToList(),
            AlternativeActions = alternativeActions
        };

        return Task.FromResult(result);
    }

    private static string DetermineNextSafeVersion(string currentVersion)
    {
        // Parse semver X.Y.Z
        var parts = currentVersion.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
        {
            // Increment minor version as safe upgrade
            return $"{major}.{minor + 1}.0";
        }

        // Fallback: just append .1
        return $"{currentVersion}.1";
    }
}
