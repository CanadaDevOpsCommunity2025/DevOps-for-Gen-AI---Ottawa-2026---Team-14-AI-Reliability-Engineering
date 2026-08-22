namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Response DTO for workflow status queries.
/// Provides comprehensive view of alert through entire approval pipeline.
/// </summary>
public class WorkflowStatusResponse
{
    /// <summary>
    /// Unique workflow identifier (also alert ID).
    /// </summary>
    [Required]
    public string WorkflowId { get; set; } = null!;

    /// <summary>
    /// Correlation ID for tracing across all related events.
    /// </summary>
    [Required]
    public string CorrelationId { get; set; } = null!;

    /// <summary>
    /// Current workflow status (Ingested, PendingApproval, Approved, Rejected).
    /// </summary>
    [Required]
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// Alert details: package, version, severity.
    /// </summary>
    public AlertSummary? Alert { get; set; }

    /// <summary>
    /// Risk assessment results.
    /// </summary>
    public RiskAssessmentSummary? RiskAssessment { get; set; }

    /// <summary>
    /// Approval decision (if any).
    /// </summary>
    public ApprovalSummary? Approval { get; set; }

    /// <summary>
    /// AI recommendation (if generated).
    /// </summary>
    public RemediationSummary? Remediation { get; set; }

    /// <summary>
    /// Next action required by user.
    /// </summary>
    [StringLength(500)]
    public string? NextStep { get; set; }

    /// <summary>
    /// When the workflow was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the workflow was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Summary of alert core details.
/// </summary>
public class AlertSummary
{
    public string Id { get; set; } = null!;
    public string PackageName { get; set; } = null!;
    public string InstalledVersion { get; set; } = null!;
    public string? FixedVersion { get; set; }
    public string ProviderSeverity { get; set; } = null!;
    public string? CveId { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Summary of risk assessment.
/// </summary>
public class RiskAssessmentSummary
{
    public int RiskScore { get; set; }
    public Severity NormalizedSeverity { get; set; }
    public string RequiredApprovalLevel { get; set; } = null!;
    public List<string> RiskFactors { get; set; } = [];
}

/// <summary>
/// Summary of approval decision.
/// </summary>
public class ApprovalSummary
{
    public ApprovalStatus Status { get; set; }
    public string? Reviewer { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

/// <summary>
/// Summary of remediation recommendation.
/// </summary>
public class RemediationSummary
{
    public string RecommendedAction { get; set; } = null!;
    public string? TargetVersion { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ModelIdentifier { get; set; } = null!;
}
