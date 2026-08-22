namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Immutable audit event recording every significant action and decision.
/// Forms the basis of compliance and governance reporting.
/// </summary>
public class AuditEvent
{
    /// <summary>
    /// Unique identifier for this audit event.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Correlation ID linking all events for a single workflow.
    /// Essential for end-to-end tracing and forensics.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;
    
    /// <summary>
    /// Reference to the workflow this event belongs to.
    /// </summary>
    [StringLength(500)]
    public string? WorkflowId { get; set; }
    
    /// <summary>
    /// Type of event (e.g., "AlertReceived", "ValidationFailed", "RiskAssessed", 
    /// "RecommendationGenerated", "ApprovalRequested", "ApprovalGranted", 
    /// "ApprovalDenied", "ActionExecuted", "Error", "PolicyViolation").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = null!;
    
    /// <summary>
    /// Severity or level of this event (Info, Warning, Error, Critical).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Level { get; set; } = "Info";
    
    /// <summary>
    /// Human-readable summary of what happened.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Summary { get; set; } = null!;
    
    /// <summary>
    /// Detailed message or description. May include sanitized context.
    /// Must NOT include secrets, full stack traces, or sensitive data.
    /// </summary>
    [StringLength(5000)]
    public string? Details { get; set; }
    
    /// <summary>
    /// Actor who caused or initiated this event.
    /// Could be a system component (e.g., "RiskEngine") or a user identity.
    /// </summary>
    [StringLength(500)]
    public string? Actor { get; set; }
    
    /// <summary>
    /// Role or service that generated the event.
    /// Examples: "AlertIngestion", "RiskEngine", "AIProvider", "ApprovalGate", "Repository".
    /// </summary>
    [StringLength(100)]
    public string? Service { get; set; }
    
    /// <summary>
    /// Associated alert ID (if applicable).
    /// </summary>
    [StringLength(500)]
    public string? AlertId { get; set; }
    
    /// <summary>
    /// Structured data captured for this event as JSON.
    /// Allows flexible event-specific fields without schema changes.
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Whether this event represents a security-relevant action requiring escalation.
    /// </summary>
    public bool IsSecurityRelevant { get; set; } = false;
}
