namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Draft pull request proposal based on a remediation recommendation.
/// This is a proposal artifact, not an actual pull request.
/// No PR is created until a human explicitly approves.
/// </summary>
public class PullRequestProposal
{
    /// <summary>
    /// Unique identifier for this proposal.
    /// </summary>
    [Required]
    public string Id { get; set; } = null!;
    
    /// <summary>
    /// Reference to the remediation recommendation.
    /// </summary>
    [Required]
    public string RecommendationId { get; set; } = null!;
    
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
    /// Proposed PR title.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ProposedTitle { get; set; } = null!;
    
    /// <summary>
    /// Proposed PR description in Markdown format.
    /// Should include context, rationale, and risk assessment summary.
    /// </summary>
    [Required]
    [StringLength(10000)]
    public string ProposedDescription { get; set; } = null!;
    
    /// <summary>
    /// List of files that should be reviewed.
    /// Typically: dependency manifest files (package.json, requirements.txt, .csproj, etc.)
    /// </summary>
    public List<string> FilesForReview { get; set; } = new();
    
    /// <summary>
    /// Specific dependency changes proposed (e.g., "update foo from 1.2.3 to 2.0.0").
    /// </summary>
    public List<string> DependencyChanges { get; set; } = new();
    
    /// <summary>
    /// Recommended commands to validate the change.
    /// Examples: "npm test", "dotnet test", "python -m pytest".
    /// </summary>
    public List<string> ValidationCommands { get; set; } = new();
    
    /// <summary>
    /// Guidance for rollback if the change causes issues.
    /// </summary>
    [StringLength(1000)]
    public string? RollbackGuidance { get; set; }
    
    /// <summary>
    /// Known limitations or open questions about this proposal.
    /// Helps reviewer understand incomplete analysis.
    /// </summary>
    [StringLength(1000)]
    public string? KnownLimitations { get; set; }
    
    /// <summary>
    /// Links to resources for further research.
    /// </summary>
    public List<string> ResourceLinks { get; set; } = new();
    
    /// <summary>
    /// Estimated effort to apply this change (e.g., "minimal", "moderate", "high").
    /// </summary>
    [StringLength(50)]
    public string? EstimatedEffort { get; set; }
    
    /// <summary>
    /// Whether the proposal is ready for human review.
    /// </summary>
    public bool IsReadyForReview { get; set; } = true;
    
    /// <summary>
    /// Raw proposal content as JSON or other structured format.
    /// Stored for archival and audit purposes.
    /// </summary>
    public string? RawProposalJson { get; set; }
    
    /// <summary>
    /// When the proposal was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
