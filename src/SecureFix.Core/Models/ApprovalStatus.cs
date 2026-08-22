namespace SecureFix.Core.Models;

/// <summary>
/// Approval workflow states for remediation actions.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Awaiting human review and decision.
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Approved by authorized reviewer. May proceed with remediation.
    /// </summary>
    Approved = 2,
    
    /// <summary>
    /// Rejected by authorized reviewer. Remediation blocked.
    /// </summary>
    Rejected = 3
}
