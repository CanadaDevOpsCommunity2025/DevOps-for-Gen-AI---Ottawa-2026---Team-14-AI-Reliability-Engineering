namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Complete workflow state and result after processing an alert.
/// Represents the entire lifecycle: ingestion → assessment → recommendation → approval → outcome.
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Unique workflow identifier.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Correlation ID for end-to-end tracing.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;
    
    /// <summary>
    /// Reference to the original alert.
    /// </summary>
    [Required]
    public string AlertId { get; set; } = null!;
    
    /// <summary>
    /// Current workflow status.
    /// </summary>
    [Required]
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Received;
    
    /// <summary>
    /// Reference to the risk assessment (if completed).
    /// </summary>
    public string? RiskAssessmentId { get; set; }
    
    /// <summary>
    /// Reference to the remediation recommendation (if generated).
    /// </summary>
    public string? RecommendationId { get; set; }
    
    /// <summary>
    /// Reference to the draft PR proposal (if generated).
    /// </summary>
    public string? ProposalId { get; set; }
    
    /// <summary>
    /// Reference to the approval decision (if made).
    /// </summary>
    public string? ApprovalDecisionId { get; set; }
    
    /// <summary>
    /// Approval status of this workflow.
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// Whether the workflow encountered an error.
    /// </summary>
    public bool HasError { get; set; } = false;
    
    /// <summary>
    /// Error message if an error occurred.
    /// Sanitized to avoid exposing secrets or stack traces.
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Whether this workflow used the AI fallback due to AI provider unavailability.
    /// </summary>
    public bool UsedAiFallback { get; set; } = false;
    
    /// <summary>
    /// Whether the kill switch was active during this workflow.
    /// If true, action adapters would be blocked even if approved.
    /// </summary>
    public bool KillSwitchActive { get; set; } = false;
    
    /// <summary>
    /// Summary description of the workflow outcome.
    /// </summary>
    [StringLength(2000)]
    public string? Summary { get; set; }
    
    /// <summary>
    /// When the workflow was initiated.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// When the workflow completed or reached a terminal state.
    /// Null if still in progress.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
    
    /// <summary>
    /// List of audit event IDs associated with this workflow.
    /// Allows quick lookup of all events for this workflow.
    /// </summary>
    public List<string> AuditEventIds { get; set; } = new();
}
