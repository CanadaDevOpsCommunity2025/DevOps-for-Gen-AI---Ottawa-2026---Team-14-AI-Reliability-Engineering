namespace SecureFix.Core.Entities;

/// <summary>
/// Entity Framework entity for RiskAssessment persistence.
/// </summary>
public class RiskAssessmentEntity
{
    public string Id { get; set; } = null!;
    public string AlertId { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public int NormalizedSeverity { get; set; }
    public int RiskScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string RiskFactorsJson { get; set; } = "[]";
    public string RequiredApprovalLevel { get; set; } = null!;
    public string? Summary { get; set; }
    public DateTimeOffset AssessedAt { get; set; }

    // Navigation properties
    public VulnerabilityAlertEntity? Alert { get; set; }
}
