namespace SecureFix.Core.Services;

using SecureFix.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Deterministic risk scoring engine for vulnerability assessment.
/// All scoring decisions are rule-based, explainable, and reproducible.
/// </summary>
public class RiskScoringEngine : IRiskScoringEngine
{
    private readonly RiskScoringPolicy _policy;

    public RiskScoringEngine(RiskScoringPolicy policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<RiskAssessment> AssessRiskAsync(VulnerabilityAlert alert)
    {
        if (alert == null)
            throw new ArgumentNullException(nameof(alert));

        // Run scoring synchronously but wrap in async to match interface
        return await Task.FromResult(ComputeRiskAssessment(alert));
    }

    private RiskAssessment ComputeRiskAssessment(VulnerabilityAlert alert)
    {
        var assessment = new RiskAssessment
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = alert.Id,
            CorrelationId = alert.CorrelationId,
            AssessedAt = DateTimeOffset.UtcNow,
            ConfidenceScore = 1.0m // Deterministic scoring has full confidence
        };

        var factors = new List<string>();
        int riskScore = 0;

        // Step 1: Get base score from provider severity
        var normalizedSeverity = NormalizeSeverity(alert.ProviderSeverity, out var baseScore);
        assessment.NormalizedSeverity = normalizedSeverity;
        riskScore = baseScore;
        factors.Add($"Base score from '{alert.ProviderSeverity}' severity: {baseScore}");

        // Step 2: Apply direct dependency bonus
        if (alert.IsDirectDependency)
        {
            riskScore += _policy.DirectDependencyBonus;
            factors.Add($"Direct dependency bonus: +{_policy.DirectDependencyBonus}");
        }
        else
        {
            factors.Add("Transitive dependency: no bonus applied");
        }

        // Step 3: Apply exploitability bonus
        if (alert.IsExploitable)
        {
            riskScore += _policy.ExploitableBonus;
            factors.Add($"Known exploitation bonus: +{_policy.ExploitableBonus}");
        }

        // Step 4: Check for available fix
        bool hasFixAvailable = !string.IsNullOrWhiteSpace(alert.FixedVersion);
        if (!hasFixAvailable)
        {
            riskScore += _policy.NoFixAvailableBonus;
            factors.Add($"No fix available bonus: +{_policy.NoFixAvailableBonus}");
        }
        else
        {
            factors.Add($"Fix available in version {alert.FixedVersion}");
        }

        // Clamp score to 0-100
        riskScore = Math.Max(0, Math.Min(100, riskScore));
        assessment.RiskScore = riskScore;

        // Determine required approval level
        assessment.RequiredApprovalLevel = DetermineApprovalLevel(riskScore);

        // Generate summary
        assessment.Summary = $"Risk score {riskScore}/100 ({assessment.NormalizedSeverity}) " +
                           $"for {alert.PackageName} {alert.InstalledVersion}. " +
                           $"Requires {assessment.RequiredApprovalLevel} approval.";

        assessment.RiskFactors = factors;

        return assessment;
    }

    private Severity NormalizeSeverity(string providerSeverity, out int baseScore)
    {
        if (string.IsNullOrWhiteSpace(providerSeverity))
        {
            baseScore = _policy.SeverityBaseScores.GetValueOrDefault("unknown", 30);
            return Severity.Medium;
        }

        var normalizedKey = providerSeverity.ToLowerInvariant();
        baseScore = _policy.SeverityBaseScores.GetValueOrDefault(normalizedKey, 30);

        return normalizedKey switch
        {
            "critical" => Severity.Critical,
            "high" => Severity.High,
            "medium" => Severity.Medium,
            "low" => Severity.Low,
            _ => Severity.Medium
        };
    }

    private string DetermineApprovalLevel(int riskScore)
    {
        if (riskScore >= _policy.AdminApprovalThreshold)
            return "Admin";

        if (riskScore >= _policy.SecurityReviewerApprovalThreshold)
            return "SecurityReviewer";

        return "SecurityReviewer"; // Default to reviewer even for low risk
    }
}
