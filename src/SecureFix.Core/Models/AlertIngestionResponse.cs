namespace SecureFix.Core.Models;

/// <summary>
/// DTO for alert ingestion response.
/// Contains workflow ID, correlation ID, and initial risk assessment.
/// </summary>
public class AlertIngestionResponse
{
    /// <summary>
    /// Unique workflow ID for tracking this alert through SecureFix.
    /// </summary>
    public required string WorkflowId { get; set; }

    /// <summary>
    /// Correlation ID for tracing through entire workflow.
    /// </summary>
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Alert was accepted and deduplicated.
    /// </summary>
    public required bool IsAccepted { get; set; }

    /// <summary>
    /// Risk assessment results (severity, score, factors).
    /// </summary>
    public required RiskAssessment Assessment { get; set; }

    /// <summary>
    /// Next workflow step (e.g., "Awaiting recommendation", "Awaiting approval").
    /// </summary>
    public required string NextStep { get; set; }

    /// <summary>
    /// Message for operator (e.g., duplicate detection, processing status).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp when alert was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
