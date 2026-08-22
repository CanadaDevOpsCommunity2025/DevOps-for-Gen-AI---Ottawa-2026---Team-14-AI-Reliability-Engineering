namespace SecureFix.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

/// <summary>
/// Pull request proposal endpoints.
/// Manages draft PR proposal generation and retrieval for approved workflows.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Produces("application/json")]
public class ProposalController : ControllerBase
{
    private readonly IPullRequestProposalService _proposalService;
    private readonly ILogger<ProposalController> _logger;

    public ProposalController(
        IPullRequestProposalService proposalService,
        ILogger<ProposalController> logger)
    {
        _proposalService = proposalService ?? throw new ArgumentNullException(nameof(proposalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate a draft PR proposal from a remediation recommendation.
    /// The proposal includes title, description, dependency changes, validation commands, and rollback guidance.
    /// Does NOT create an actual GitHub PR - this is purely for human review.
    /// </summary>
    /// <param name="id">Workflow/Alert ID.</param>
    /// <returns>Generated PR proposal artifact.</returns>
    /// <response code="201">Proposal generated and persisted successfully.</response>
    /// <response code="400">Invalid request (recommendation not found, already has proposal).</response>
    /// <response code="500">Server error during proposal generation.</response>
    [HttpPost("{id}/proposal")]
    [ProducesResponseType(typeof(PullRequestProposal), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateProposal(string id)
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
            _logger.LogInformation("Generating PR proposal for workflow {WorkflowId}", id);

            var proposal = await _proposalService.GenerateProposalAsync(id);

            return CreatedAtAction(
                nameof(GetProposal),
                new { id },
                proposal
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Proposal generation failed for {WorkflowId}: {Message}", id, ex.Message);

            // Determine HTTP status based on error message
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Recommendation Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/recommendation-not-found"
                });
            }

            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Proposal Already Exists",
                    Status = StatusCodes.Status409Conflict,
                    Detail = ex.Message,
                    Type = "https://securefix.example.com/errors/proposal-already-exists"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Proposal Generation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Type = "https://securefix.example.com/errors/proposal-generation-failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating proposal for workflow {WorkflowId}", id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred while generating the proposal.",
                    Type = "https://securefix.example.com/errors/internal-error"
                }
            );
        }
    }

    /// <summary>
    /// Retrieve an existing PR proposal by recommendation ID.
    /// </summary>
    /// <param name="id">Workflow/Recommendation ID.</param>
    /// <returns>The PR proposal artifact if it exists.</returns>
    /// <response code="200">Proposal found and returned.</response>
    /// <response code="404">Proposal not found.</response>
    /// <response code="500">Server error during retrieval.</response>
    [HttpGet("{id}/proposal")]
    [ProducesResponseType(typeof(PullRequestProposal), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProposal(string id)
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
            _logger.LogInformation("Retrieving PR proposal for recommendation {RecommendationId}", id);

            var proposal = await _proposalService.GetProposalAsync(id);

            if (proposal == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Proposal Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No PR proposal exists for recommendation {id}.",
                    Type = "https://securefix.example.com/errors/proposal-not-found"
                });
            }

            return Ok(proposal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving proposal for recommendation {RecommendationId}", id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred while retrieving the proposal.",
                    Type = "https://securefix.example.com/errors/internal-error"
                }
            );
        }
    }
}
