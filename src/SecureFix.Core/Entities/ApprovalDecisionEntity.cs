namespace SecureFix.Core.Entities;

/// <summary>
/// Entity Framework entity for ApprovalDecision persistence.
/// </summary>
public class ApprovalDecisionEntity
{
    public string Id { get; set; } = null!;
    public string WorkflowId { get; set; } = null!;
    public string AlertId { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public int Status { get; set; }
    public string ReviewerIdentity { get; set; } = null!;
    public string ReviewerRole { get; set; } = null!;
    public string? Reason { get; set; }
    public string? Comments { get; set; }
    public DateTimeOffset DecisionTime { get; set; }
    public string? ClientIdentifier { get; set; }
}
