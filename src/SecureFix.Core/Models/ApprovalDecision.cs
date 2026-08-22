namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Human approval decision on a remediation recommendation.
/// This is the mandatory gate that must pass before any action proceeds.
/// </summary>
public class ApprovalDecision
{
    /// <summary>
    /// Unique identifier for this decision.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Reference to the workflow being approved.
    /// </summary>
    [Required]
    public string WorkflowId { get; set; } = null!;
    
    /// <summary>
    /// Reference to the alert.
    /// </summary>
    [Required]
    public string AlertId { get; set; } = null!;
    
    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;
    
    /// <summary>
    /// Approval or rejection decision.
    /// </summary>
    [Required]
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// Identity of the reviewer who made the decision.
    /// For this MVP, could be a mock identity like "reviewer@example.com" or a system ID.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ReviewerIdentity { get; set; } = null!;
    
    /// <summary>
    /// Role of the reviewer (e.g., "SecurityReviewer", "Admin").
    /// Confirms that reviewer had proper authorization.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ReviewerRole { get; set; } = null!;
    
    /// <summary>
    /// Reason for the decision. Required for rejected approvals.
    /// Helpful context for auditing and future decisions.
    /// </summary>
    [StringLength(1000)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// Additional notes or context from the reviewer.
    /// </summary>
    [StringLength(2000)]
    public string? Comments { get; set; }
    
    /// <summary>
    /// When the decision was made.
    /// </summary>
    public DateTimeOffset DecisionTime { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// IP address or client identifier of the reviewer (for audit purposes).
    /// May be null if not available.
    /// </summary>
    [StringLength(100)]
    public string? ClientIdentifier { get; set; }
}
