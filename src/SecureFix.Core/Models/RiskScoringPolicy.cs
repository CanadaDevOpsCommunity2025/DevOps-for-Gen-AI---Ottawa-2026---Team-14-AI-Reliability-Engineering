namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for risk scoring thresholds and policy rules.
/// Kept separate from code to allow policy changes without recompilation.
/// </summary>
public class RiskScoringPolicy
{
    /// <summary>
    /// Minimum risk score (0-100) to be classified as Critical severity.
    /// </summary>
    [Range(0, 100)]
    public int CriticalMinimumScore { get; set; } = 90;
    
    /// <summary>
    /// Minimum risk score to be classified as High severity.
    /// </summary>
    [Range(0, 100)]
    public int HighMinimumScore { get; set; } = 70;
    
    /// <summary>
    /// Minimum risk score to be classified as Medium severity.
    /// </summary>
    [Range(0, 100)]
    public int MediumMinimumScore { get; set; } = 40;
    
    /// <summary>
    /// Score points added for a direct dependency.
    /// </summary>
    public int DirectDependencyBonus { get; set; } = 15;
    
    /// <summary>
    /// Score points added if vulnerability is known to be actively exploited.
    /// </summary>
    public int ExploitableBonus { get; set; } = 20;
    
    /// <summary>
    /// Score points added if no fixed version is available.
    /// </summary>
    public int NoFixAvailableBonus { get; set; } = 10;
    
    /// <summary>
    /// Base score for each provider severity level.
    /// Maps provider severity strings to numeric scores.
    /// </summary>
    public Dictionary<string, int> SeverityBaseScores { get; set; } = new()
    {
        { "critical", 80 },
        { "high", 60 },
        { "medium", 40 },
        { "low", 20 },
        { "info", 5 },
        { "unknown", 30 }
    };
    
    /// <summary>
    /// Minimum score to require Admin-level approval.
    /// </summary>
    [Range(0, 100)]
    public int AdminApprovalThreshold { get; set; } = 85;
    
    /// <summary>
    /// Minimum score to require SecurityReviewer-level approval.
    /// </summary>
    [Range(0, 100)]
    public int SecurityReviewerApprovalThreshold { get; set; } = 40;
}
