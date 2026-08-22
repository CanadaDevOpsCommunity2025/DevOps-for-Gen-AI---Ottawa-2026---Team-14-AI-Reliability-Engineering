namespace SecureFix.Core.Models;

/// <summary>
/// Severity levels for vulnerabilities, normalized across all providers.
/// </summary>
public enum Severity
{
    /// <summary>
    /// Lowest severity - may include cosmetic or non-security issues.
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Medium severity - requires evaluation but not immediately critical.
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// High severity - should be addressed promptly in most deployments.
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Critical severity - immediate remediation strongly recommended.
    /// </summary>
    Critical = 4
}
