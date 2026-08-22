namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Result of deterministic risk assessment for a vulnerability alert.
/// Computed by the risk engine without AI involvement.
/// </summary>
public class RiskAssessment
{
    /// <summary>
    /// Unique identifier for this assessment.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Reference to the alert being assessed.
    /// </summary>
    [Required]
    public string AlertId { get; set; } = null!;
    
    /// <summary>
    /// Correlation ID inherited from the alert for tracing.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;
    
    /// <summary>
    /// Normalized severity (Critical, High, Medium, Low).
    /// </summary>
    [Required]
    public Severity NormalizedSeverity { get; set; }
    
    /// <summary>
    /// Numeric risk score from 0 (lowest) to 100 (highest).
    /// </summary>
    [Range(0, 100)]
    public int RiskScore { get; set; }
    
    /// <summary>
    /// Confidence in this assessment (0.0 = no confidence, 1.0 = complete confidence).
    /// </summary>
    [Range(0, 1)]
    public decimal ConfidenceScore { get; set; } = 1.0m;
    
    /// <summary>
    /// List of factors that contributed to the risk score.
    /// Each entry explains why the score was adjusted up or down.
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();
    
    /// <summary>
    /// Approval level required for this alert (e.g., "SecurityReviewer", "Admin").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RequiredApprovalLevel { get; set; } = "SecurityReviewer";
    
    /// <summary>
    /// Human-readable summary of the risk assessment.
    /// </summary>
    [StringLength(1000)]
    public string? Summary { get; set; }
    
    /// <summary>
    /// When the assessment was performed.
    /// </summary>
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
}
