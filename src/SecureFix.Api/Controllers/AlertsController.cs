namespace SecureFix.Api.Controllers;

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFix.Core.Models;
using SecureFix.Core.Services;

/// <summary>
/// Alert ingestion endpoint.
/// Accepts vulnerability alerts from Dependabot, GitHub Security, or other sources.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertIngestionService _ingestionService;
    private readonly IValidator<AlertIngestionRequest> _validator;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IAlertIngestionService ingestionService,
        IValidator<AlertIngestionRequest> validator,
        ILogger<AlertsController> logger)
    {
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingest a vulnerability alert.
    /// </summary>
    /// <param name="request">Alert details (package, version, severity, etc).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ingestion response with workflow ID and risk assessment.</returns>
    /// <response code="202">Alert accepted for processing.</response>
    /// <response code="400">Invalid alert request (validation failed).</response>
    /// <response code="409">Duplicate alert (already processed).</response>
    /// <response code="500">Server error during ingestion.</response>
    [Authorize(Roles = "Developer,SecurityReviewer,Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(AlertIngestionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestAlert(
        [FromBody] AlertIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received alert ingestion request");

        // Validate request
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Alert validation failed: {Errors}", string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            var problemDetails = new ValidationProblemDetails
            {
                Title = "Alert Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The alert request contains validation errors."
            };

            foreach (var error in validation.Errors.GroupBy(e => e.PropertyName))
            {
                problemDetails.Errors[error.Key] = error.Select(e => e.ErrorMessage).ToArray();
            }

            return BadRequest(problemDetails);
        }

        try
        {
            var response = await _ingestionService.IngestAlertAsync(request, cancellationToken);
            if (!response.IsAccepted)
            {
                _logger.LogInformation("Duplicate alert detected. Returning original workflow.");
                return Conflict(response);
            }

            return Accepted($"/api/v1/workflows/{response.WorkflowId}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert ingestion failed");

            var problemDetails = new ProblemDetails
            {
                Title = "Alert Ingestion Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred during alert ingestion.",
                Type = "https://securefix.example.com/errors/ingestion-failed"
            };

            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }
    }

    /// <summary>
    /// Check if an alert has been previously ingested (duplicate detection).
    /// </summary>
    /// <param name="externalAlertId">External alert ID to check.</param>
    /// <returns>True if alert has been ingested, false otherwise.</returns>
    [Authorize(Roles = "Developer,SecurityReviewer,Admin")]
    [HttpHead("{externalAlertId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckAlertExists(string externalAlertId)
    {
        var isDuplicate = await _ingestionService.IsDuplicateAsync(externalAlertId);
        return isDuplicate ? Ok() : NotFound();
    }
}
