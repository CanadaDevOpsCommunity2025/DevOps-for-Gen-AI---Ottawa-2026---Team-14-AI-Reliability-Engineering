namespace SecureFix.Core.Entities;

/// <summary>
/// Entity Framework entity for AuditEvent persistence.
/// Immutable audit trail of all significant actions.
/// </summary>
public class AuditEventEntity
{
    public string Id { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public string? WorkflowId { get; set; }
    public string EventType { get; set; } = null!;
    public string Level { get; set; } = "Info";
    public string Summary { get; set; } = null!;
    public string? Details { get; set; }
    public string? Actor { get; set; }
    public string? Service { get; set; }
    public string? AlertId { get; set; }
    public string? Metadata { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public bool IsSecurityRelevant { get; set; }

    // Navigation properties
    public VulnerabilityAlertEntity? Alert { get; set; }
}
