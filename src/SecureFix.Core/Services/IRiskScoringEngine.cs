namespace SecureFix.Core.Services;

using SecureFix.Core.Models;

/// <summary>
/// Contract for deterministic risk assessment engine.
/// Risk scoring happens without AI involvement and is fully explainable.
/// </summary>
public interface IRiskScoringEngine
{
    /// <summary>
    /// Assess the risk of a vulnerability alert.
    /// </summary>
    /// <param name="alert">The vulnerability alert to assess.</param>
    /// <returns>A complete risk assessment with scoring rationale.</returns>
    Task<RiskAssessment> AssessRiskAsync(VulnerabilityAlert alert);
}
