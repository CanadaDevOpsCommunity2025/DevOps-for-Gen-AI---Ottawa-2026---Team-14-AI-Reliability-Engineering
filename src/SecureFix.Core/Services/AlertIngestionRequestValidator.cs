namespace SecureFix.Core.Services;

using System.Text.RegularExpressions;
using FluentValidation;
using SecureFix.Core.Models;

/// <summary>
/// Validation rules for alert ingestion requests.
/// Treats all external input as untrusted.
/// </summary>
public class AlertIngestionRequestValidator : AbstractValidator<AlertIngestionRequest>
{
    public AlertIngestionRequestValidator()
    {
        RuleFor(x => x.ExternalAlertId)
            .NotEmpty().WithMessage("External alert ID is required")
            .MaximumLength(500).WithMessage("External alert ID must be 500 characters or less");

        RuleFor(x => x.PackageName)
            .NotEmpty().WithMessage("Package name is required")
            .MaximumLength(500).WithMessage("Package name must be 500 characters or less");

        RuleFor(x => x.InstalledVersion)
            .NotEmpty().WithMessage("Installed version is required")
            .MaximumLength(100).WithMessage("Version must be 100 characters or less");

        RuleFor(x => x.ProviderSeverity)
            .NotEmpty().WithMessage("Severity is required")
            .MaximumLength(20).WithMessage("Severity must be 20 characters or less")
            .Must(BeValidSeverity).WithMessage("Severity must be one of: critical, high, medium, low, info");

        RuleFor(x => x.CveId)
            .MaximumLength(50).WithMessage("CVE ID must be 50 characters or less")
            .Matches(new Regex(@"^(CVE-\d{4}-\d{4,})?$", RegexOptions.IgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.CveId))
            .WithMessage("CVE ID must be in format CVE-YYYY-NNNNN");

        RuleFor(x => x.FixedVersion)
            .MaximumLength(100).WithMessage("Fixed version must be 100 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must be 5000 characters or less");

        RuleFor(x => x.RepositoryIdentifier)
            .MaximumLength(500).WithMessage("Repository identifier must be 500 characters or less");

        RuleFor(x => x.AdvisoryUrl)
            .MaximumLength(2000).WithMessage("Advisory URL must be 2000 characters or less")
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.AdvisoryUrl))
            .WithMessage("Advisory URL must be a valid URL");
    }

    private bool BeValidSeverity(string? severity)
    {
        if (string.IsNullOrEmpty(severity))
            return true;

        var validSeverities = new[] { "critical", "high", "medium", "low", "info" };
        return validSeverities.Contains(severity.ToLowerInvariant());
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var _);
    }
}
