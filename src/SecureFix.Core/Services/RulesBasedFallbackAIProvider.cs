namespace SecureFix.Core.Services;

using SecureFix.Core.Models;

/// <summary>
/// Fallback provider using deterministic rules when AI is unavailable.
/// Provides safe, explainable recommendations without external dependencies.
/// </summary>
public class RulesBasedFallbackAIProvider : IAIRecommendationProvider
{
    public string ProviderIdentifier => "rules-based-fallback";

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }

    public Task<AIRecommendationResult> RecommendAsync(
        VulnerabilityAlert alert,
        RiskAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        // Rule 1: Score-based recommendation
        string recommendedAction;
        string? targetVersion;
        string explanation;
        int confidenceScore;

        if (assessment.RiskScore >= 85)
        {
            recommendedAction = "Upgrade";
            targetVersion = $"{GetMajorVersion(alert.InstalledVersion)}.{GetMinorVersion(alert.InstalledVersion) + 1}.0";
            explanation =
                $"Risk score {assessment.RiskScore} (≥85) indicates critical vulnerability. " +
                $"Rules-based recommendation: upgrade to next minor version. " +
                $"Human approval required.";
            confidenceScore = 60;
        }
        else if (assessment.RiskScore >= 60)
        {
            recommendedAction = "Schedule";
            targetVersion = $"{GetMajorVersion(alert.InstalledVersion)}.{GetMinorVersion(alert.InstalledVersion) + 1}.0";
            explanation =
                $"Risk score {assessment.RiskScore} (60-84) indicates high impact. " +
                $"Rules-based recommendation: schedule upgrade for next release. " +
                $"Human approval required.";
            confidenceScore = 50;
        }
        else if (assessment.RiskScore >= 40)
        {
            recommendedAction = "Monitor";
            targetVersion = null;
            explanation =
                $"Risk score {assessment.RiskScore} (40-59) indicates moderate risk. " +
                $"Rules-based recommendation: monitor for updates. " +
                $"Plan upgrade during next version bump.";
            confidenceScore = 40;
        }
        else
        {
            recommendedAction = "Monitor";
            targetVersion = null;
            explanation =
                $"Risk score {assessment.RiskScore} (<40) indicates low risk. " +
                $"Rules-based recommendation: continue standard monitoring. " +
                $"Upgrade only during major version transitions.";
            confidenceScore = 30;
        }

        // Rule 2: Direct dependency bonus
        if (alert.IsDirectDependency)
        {
            explanation += " [Direct dependency: higher priority]";
        }

        // Rule 3: Exploitability bonus
        if (alert.IsExploitable)
        {
            explanation += " [Known exploits exist: expedite upgrade]";
        }

        var result = new AIRecommendationResult
        {
            ModelIdentifier = ProviderIdentifier,
            RecommendedAction = recommendedAction,
            TargetVersion = targetVersion,
            Explanation = explanation,
            Disclaimer =
                "AI provider unavailable. Using rule-based fallback. " +
                "This recommendation is based on score and risk factors alone. " +
                "Human review is mandatory.",
            PromptVersion = "rules-v1",
            ConfidenceScore = confidenceScore,
            RiskFactors = assessment.RiskFactors.ToList(),
            AlternativeActions = []
        };

        return Task.FromResult(result);
    }

    private static int GetMajorVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length > 0 && int.TryParse(parts[0], out var major))
        {
            return major;
        }

        return 1;
    }

    private static int GetMinorVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length > 1 && int.TryParse(parts[1], out var minor))
        {
            return minor;
        }

        return 0;
    }
}
