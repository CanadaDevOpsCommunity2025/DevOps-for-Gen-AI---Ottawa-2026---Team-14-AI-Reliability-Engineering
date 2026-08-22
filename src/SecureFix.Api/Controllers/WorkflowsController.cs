namespace SecureFix.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

/// <summary>
/// Workflow and approval endpoints.
/// Manages workflow status queries and approval decisions.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class WorkflowsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IApprovalService approvalService,
        ILogger<WorkflowsController> logger)
    {
        _approvalService = approvalService ?? throw new ArgumentNullException(nameof(approvalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get workflow status for an alert.
    /// </summary>
    /// <param name="id">Alert/Workflow ID.</param>
    /// <returns>Workflow status including alert, risk assessment, and approval state.</returns>
    /// <response code="200">Workflow found and returned.</response>
    /// <response code="404">Workflow not found.</response>
    /// <response code="500">Server error during lookup.</response>
    [Authorize(Roles = "Developer,SecurityReviewer,Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWorkflowStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Workflow ID",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Workflow ID cannot be empty."
            });
        }

        try
        {
            _logger.LogInformation("Fetching workflow status: {WorkflowId}", id);

            var status = await _approvalService.GetWorkflowStatusAsync(id);
            if (status == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Workflow Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No workflow found with ID: {id}",
                    Type = "https://securefix.example.com/errors/workflow-not-found"
                });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow status: {WorkflowId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Workflow Lookup Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred while retrieving workflow status.",
                Type = "https://securefix.example.com/errors/workflow-lookup-failed"
            });
        }
    }

    /// <summary>
    /// Approve a workflow for remediation.
    /// Only SecurityReviewer role is authorized.
    /// </summary>
    /// <param name="id">Alert/Workflow ID.</param>
    /// <param name="request">Approval details (reviewer, optional reason).</param>
    /// <returns>Updated workflow status.</returns>
    /// <response code="200">Workflow approved successfully.</response>
    /// <response code="400">Invalid request (validation failed, already decided, workflow not found).</response>
    /// <response code="403">Unauthorized (caller is not SecurityReviewer).</response>
    /// <response code="409">Workflow already has an approval decision.</response>
    /// <response code="500">Server error during approval.</response>
    [Authorize(Roles = "SecurityReviewer,Admin")]
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveWorkflow(
        string id,
        [FromBody] ApprovalRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Workflow ID",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Workflow ID cannot be empty."
            });
        }

        if (request == null || request.Decision?.ToLower() != "approved")
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Approval Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request must have decision='approved'."
            });
        }

        var reviewerRole = User.FindFirstValue(ClaimTypes.Role) ?? request.ReviewerRole ?? "SecurityReviewer";
        if (string.IsNullOrWhiteSpace(request.Reviewer))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Reviewer",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Reviewer identity is required."
            });
        }

        try
        {
            _logger.LogInformation("Approving workflow {WorkflowId} by {Reviewer} as {Role}", id, request.Reviewer, reviewerRole);

            var updatedStatus = await _approvalService.ApproveAlertAsync(id, request.Reviewer, request.Reason, reviewerRole);

            return Ok(updatedStatus);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized approval attempt for workflow {WorkflowId}: {Message}", id, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Approval failed for {WorkflowId}: {Message}", id, ex.Message);

            // Determine HTTP status based on error message
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Workflow Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-not-found"
                });
            }

            if (ex.Message.Contains("already has a decision"))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Workflow Already Decided",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-already-decided"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Approval Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving workflow: {WorkflowId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Approval Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred during approval.",
                Type = "https://securefix.example.com/errors/approval-failed"
            });
        }
    }

    /// <summary>
    /// Reject a workflow, blocking any further remediation.
    /// Only SecurityReviewer role is authorized.
    /// </summary>
    /// <param name="id">Alert/Workflow ID.</param>
    /// <param name="request">Rejection details (reviewer, optional reason).</param>
    /// <returns>Updated workflow status (Rejected).</returns>
    /// <response code="200">Workflow rejected successfully.</response>
    /// <response code="400">Invalid request (validation failed, already decided, workflow not found).</response>
    /// <response code="403">Unauthorized (caller is not SecurityReviewer).</response>
    /// <response code="409">Workflow already has an approval decision.</response>
    /// <response code="500">Server error during rejection.</response>
    [Authorize(Roles = "SecurityReviewer,Admin")]
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectWorkflow(
        string id,
        [FromBody] ApprovalRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Workflow ID",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Workflow ID cannot be empty."
            });
        }

        if (request == null || request.Decision?.ToLower() != "rejected")
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Rejection Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request must have decision='rejected'."
            });
        }

        var reviewerRole = User.FindFirstValue(ClaimTypes.Role) ?? request.ReviewerRole ?? "SecurityReviewer";
        if (string.IsNullOrWhiteSpace(request.Reviewer))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Reviewer",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Reviewer identity is required."
            });
        }

        try
        {
            _logger.LogInformation("Rejecting workflow {WorkflowId} by {Reviewer} as {Role}", id, request.Reviewer, reviewerRole);

            var updatedStatus = await _approvalService.RejectAlertAsync(id, request.Reviewer, request.Reason, reviewerRole);

            return Ok(updatedStatus);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized rejection attempt for workflow {WorkflowId}: {Message}", id, ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rejection failed for {WorkflowId}: {Message}", id, ex.Message);

            // Determine HTTP status based on error message
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Workflow Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-not-found"
                });
            }

            if (ex.Message.Contains("already has a decision"))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Workflow Already Decided",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-already-decided"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Rejection Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting workflow: {WorkflowId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Rejection Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred during rejection.",
                Type = "https://securefix.example.com/errors/rejection-failed"
            });
        }
    }
}
