namespace SecureFix.Core.Models;

/// <summary>
/// DTO for incoming vulnerability alert from external source.
/// Mapped from Dependabot, GitHub Security, or other scanners.
/// Treated as untrusted input.
/// </summary>
public class AlertIngestionRequest
{
    /// <summary>
    /// External alert ID for deduplication (required).
    /// </summary>
    public string? ExternalAlertId { get; set; }

    /// <summary>
    /// CVE identifier if available (optional).
    /// </summary>
    public string? CveId { get; set; }

    /// <summary>
    /// Package/component name (required, untrusted).
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Currently installed version (required).
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    /// Fixed/safe version if available (optional).
    /// </summary>
    public string? FixedVersion { get; set; }

    /// <summary>
    /// Severity as reported by external scanner (required).
    /// </summary>
    public string? ProviderSeverity { get; set; }

    /// <summary>
    /// Vulnerability description (optional, untrusted).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Is this a direct or transitive dependency? (optional, defaults to true).
    /// </summary>
    public bool? IsDirectDependency { get; set; }

    /// <summary>
    /// Is vulnerability actively exploited? (optional, defaults to false).
    /// </summary>
    public bool? IsExploitable { get; set; }

    /// <summary>
    /// Repository/project identifier (optional).
    /// </summary>
    public string? RepositoryIdentifier { get; set; }

    /// <summary>
    /// URL to advisory (optional).
    /// </summary>
    public string? AdvisoryUrl { get; set; }
}
