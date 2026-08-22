namespace SecureFix.Core.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for approval decisions.
/// </summary>
public class ApprovalRequestDto
{
    /// <summary>
    /// Reviewer identity (user ID or email).
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Reviewer { get; set; } = null!;

    /// <summary>
    /// Role of the reviewer submitting the decision.
    /// Only SecurityReviewer and Admin are allowed to approve or reject workflow actions.
    /// </summary>
    [StringLength(100)]
    public string? ReviewerRole { get; set; }

    /// <summary>
    /// Approval decision: "approved" or "rejected".
    /// </summary>
    [Required]
    [RegularExpression(@"^(approved|rejected)$", ErrorMessage = "Decision must be 'approved' or 'rejected'")]
    public string Decision { get; set; } = null!;

    /// <summary>
    /// Reason for decision (optional for approval, recommended for rejection).
    /// </summary>
    [StringLength(1000)]
    public string? Reason { get; set; }
}
