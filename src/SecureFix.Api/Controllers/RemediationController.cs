namespace SecureFix.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

/// <summary>
/// Remediation recommendation endpoints.
/// Manages AI-powered remediation suggestions for approved vulnerabilities.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Produces("application/json")]
public class RemediationController : ControllerBase
{
    private readonly IRemediationRecommendationService _remediationService;
    private readonly ILogger<RemediationController> _logger;

    public RemediationController(
        IRemediationRecommendationService remediationService,
        ILogger<RemediationController> logger)
    {
        _remediationService = remediationService ?? throw new ArgumentNullException(nameof(remediationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate an AI-powered remediation recommendation for an approved workflow.
    /// Workflow must be approved before recommendation can be generated.
    /// Calls the configured AI provider (Azure AI Foundry, Mock, or Rules-Based Fallback).
    /// </summary>
    /// <param name="id">Workflow/Alert ID.</param>
    /// <returns>Generated remediation recommendation with AI metadata.</returns>
    /// <response code="201">Recommendation generated and persisted successfully.</response>
    /// <response code="400">Invalid request (workflow not found, not approved, or already has recommendation).</response>
    /// <response code="500">Server error during recommendation generation.</response>
    [HttpPost("{id}/remediate")]
    [ProducesResponseType(typeof(RemediationRecommendation), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateRecommendation(string id)
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
            _logger.LogInformation("Generating remediation recommendation for workflow {WorkflowId}", id);

            var recommendation = await _remediationService.GenerateRecommendationAsync(id);

            return CreatedAtAction(
                nameof(GetRecommendation),
                new { id },
                recommendation
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Recommendation generation failed for {WorkflowId}: {Message}", id, ex.Message);

            // Determine HTTP status based on error message
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Workflow Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-not-found"
                });
            }

            if (ex.Message.Contains("not approved", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Workflow Not Approved",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/workflow-not-approved"
                });
            }

            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Recommendation Already Exists",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/recommendation-already-exists"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Recommendation Generation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating remediation recommendation for workflow {WorkflowId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Recommendation Generation Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred while generating remediation recommendation.",
                Type = "https://securefix.example.com/errors/recommendation-generation-failed"
            });
        }
    }

    /// <summary>
    /// Get an existing remediation recommendation for a workflow.
    /// </summary>
    /// <param name="id">Workflow/Alert ID.</param>
    /// <returns>Remediation recommendation if it exists.</returns>
    /// <response code="200">Recommendation found and returned.</response>
    /// <response code="404">Workflow not found or no recommendation exists.</response>
    /// <response code="500">Server error during lookup.</response>
    [HttpGet("{id}/remediate")]
    [ProducesResponseType(typeof(RemediationRecommendation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecommendation(string id)
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
            _logger.LogInformation("Fetching remediation recommendation for workflow {WorkflowId}", id);

            var recommendation = await _remediationService.GetRecommendationAsync(id);
            if (recommendation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Recommendation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No recommendation found for workflow {id}.",
                    Type = "https://securefix.example.com/errors/recommendation-not-found"
                });
            }

            return Ok(recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving remediation recommendation for workflow {WorkflowId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Recommendation Lookup Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred while retrieving recommendation.",
                Type = "https://securefix.example.com/errors/recommendation-lookup-failed"
            });
        }
    }
}
