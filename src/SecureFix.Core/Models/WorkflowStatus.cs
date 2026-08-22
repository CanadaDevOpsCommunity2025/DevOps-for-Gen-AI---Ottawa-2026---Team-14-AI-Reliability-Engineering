namespace SecureFix.Core.Models;

/// <summary>
/// Workflow processing states for tracking remediation lifecycle.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Alert received by ingestion API.
    /// </summary>
    Received = 1,
    
    /// <summary>
    /// Payload validated and normalized.
    /// </summary>
    Validated = 2,
    
    /// <summary>
    /// Risk assessment complete.
    /// </summary>
    Assessed = 3,
    
    /// <summary>
    /// Remediation recommendation generated.
    /// </summary>
    Recommended = 4,
    
    /// <summary>
    /// Awaiting human approval decision.
    /// </summary>
    PendingApproval = 5,
    
    /// <summary>
    /// Approved and ready for remediation.
    /// </summary>
    Approved = 6,
    
    /// <summary>
    /// Rejected by reviewer.
    /// </summary>
    Rejected = 7,
    
    /// <summary>
    /// Workflow encountered an error.
    /// </summary>
    Failed = 8
}
