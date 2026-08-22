namespace SecureFix.Core.Services;

using Microsoft.Extensions.Logging;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;

/// <summary>
/// Contract for alert ingestion service.
/// Handles validation, deduplication, risk assessment, and persistence.
/// </summary>
public interface IAlertIngestionService
{
    /// <summary>
    /// Ingest a vulnerability alert from external source.
    /// </summary>
    /// <param name="request">Validated alert ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ingestion response with workflow ID and initial assessment.</returns>
    Task<AlertIngestionResponse> IngestAlertAsync(
        AlertIngestionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if alert is duplicate (already processed).
    /// </summary>
    Task<bool> IsDuplicateAsync(string externalAlertId);
}

/// <summary>
/// Alert ingestion service implementation.
/// Orchestrates validation, deduplication, assessment, and persistence.
/// </summary>
public class AlertIngestionService : IAlertIngestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRiskScoringEngine _riskScoringEngine;
    private readonly ILogger<AlertIngestionService> _logger;

    public AlertIngestionService(
        IUnitOfWork unitOfWork,
        IRiskScoringEngine riskScoringEngine,
        ILogger<AlertIngestionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _riskScoringEngine = riskScoringEngine ?? throw new ArgumentNullException(nameof(riskScoringEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsDuplicateAsync(string externalAlertId)
    {
        var existing = await _unitOfWork.VulnerabilityAlerts.GetByExternalIdAsync(externalAlertId);
        return existing != null;
    }

    public async Task<AlertIngestionResponse> IngestAlertAsync(
        AlertIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.ExternalAlertId);
        ArgumentNullException.ThrowIfNull(request.PackageName);
        ArgumentNullException.ThrowIfNull(request.InstalledVersion);
        ArgumentNullException.ThrowIfNull(request.ProviderSeverity);

        // Step 1: Check for duplicate before creating any new workflow or correlation IDs.
        var existingAlert = await _unitOfWork.VulnerabilityAlerts.GetByExternalIdAsync(request.ExternalAlertId);
        if (existingAlert != null)
        {
            _logger.LogWarning(
                "Duplicate alert detected: {ExternalId}. Reusing original workflow {WorkflowId} and correlation {CorrelationId}",
                request.ExternalAlertId,
                existingAlert.Id,
                existingAlert.CorrelationId);

            var existingAssessment = await _unitOfWork.RiskAssessments.GetByAlertIdAsync(existingAlert.Id);
            return new AlertIngestionResponse
            {
                WorkflowId = existingAlert.Id,
                CorrelationId = existingAlert.CorrelationId,
                IsAccepted = false,
                Assessment = existingAssessment != null
                    ? MapEntityToModel(existingAssessment)
                    : new RiskAssessment
                    {
                        AlertId = existingAlert.Id,
                        CorrelationId = existingAlert.CorrelationId,
                        NormalizedSeverity = Severity.Medium,
                        RiskScore = 50,
                        RiskFactors = ["duplicate-detected"]
                    },
                NextStep = "Skipped (duplicate)",
                Message = $"This alert was already ingested. Original workflow ID: {existingAlert.Id}. Original correlation ID: {existingAlert.CorrelationId}",
                ReceivedAt = existingAlert.ReceivedAt
            };
        }

        var alertId = Guid.NewGuid().ToString();
        var workflowId = alertId;
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Ingesting alert {WorkflowId}: {Package}@{Version} (external ID: {ExternalId})",
            workflowId,
            request.PackageName,
            request.InstalledVersion,
            request.ExternalAlertId);

        // Step 2: Create VulnerabilityAlert domain model
        var alert = new VulnerabilityAlert
        {
            Id = alertId,
            CorrelationId = correlationId,
            ExternalAlertId = request.ExternalAlertId,
            CveId = request.CveId,
            PackageName = request.PackageName,
            InstalledVersion = request.InstalledVersion,
            FixedVersion = request.FixedVersion,
            ProviderSeverity = request.ProviderSeverity,
            Description = request.Description,
            IsDirectDependency = request.IsDirectDependency ?? true,
            IsExploitable = request.IsExploitable ?? false,
            RepositoryIdentifier = request.RepositoryIdentifier,
            AdvisoryUrl = request.AdvisoryUrl,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        // Step 3: Compute risk assessment
        var assessment = await _riskScoringEngine.AssessRiskAsync(alert);
        assessment.AlertId = alertId;
        assessment.CorrelationId = correlationId;

        // Step 4: Create audit event
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            EventType = "AlertIngested",
            Details = $"Alert ingested: {request.PackageName}@{request.InstalledVersion} (CVE: {request.CveId ?? "N/A"})",
            IsSecurityRelevant = true,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "SystemAlertIngestion"
        };

        // Step 5: Persist via UnitOfWork
        try
        {
            await _unitOfWork.VulnerabilityAlerts.AddAsync(MapDomainToAlertEntity(alert));
            await _unitOfWork.RiskAssessments.AddAsync(MapDomainToRiskEntity(assessment));
            await _unitOfWork.AuditEvents.AddAsync(MapDomainToAuditEntity(auditEvent));
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Alert ingested successfully. WorkflowId: {WorkflowId}, CorrelationId: {CorrelationId}, RiskScore: {Score}",
                workflowId,
                correlationId,
                assessment.RiskScore);

            return new AlertIngestionResponse
            {
                WorkflowId = workflowId,
                CorrelationId = correlationId,
                IsAccepted = true,
                Assessment = assessment,
                NextStep = "Awaiting AI recommendation",
                Message = $"Alert accepted. Risk score: {assessment.RiskScore}/100",
                ReceivedAt = alert.ReceivedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to ingest alert {CorrelationId}. Rollback initiated.",
                correlationId);

            throw;
        }
    }

    private static Entities.VulnerabilityAlertEntity MapDomainToAlertEntity(VulnerabilityAlert domain)
    {
        return new Entities.VulnerabilityAlertEntity
        {
            Id = domain.Id,
            CorrelationId = domain.CorrelationId,
            ExternalAlertId = domain.ExternalAlertId,
            CveId = domain.CveId,
            PackageName = domain.PackageName,
            InstalledVersion = domain.InstalledVersion,
            FixedVersion = domain.FixedVersion,
            ProviderSeverity = domain.ProviderSeverity,
            Description = domain.Description,
            IsDirectDependency = domain.IsDirectDependency,
            IsExploitable = domain.IsExploitable,
            RepositoryIdentifier = domain.RepositoryIdentifier,
            AdvisoryUrl = domain.AdvisoryUrl,
            ReceivedAt = domain.ReceivedAt
        };
    }

    private static Entities.RiskAssessmentEntity MapDomainToRiskEntity(RiskAssessment domain)
    {
        return new Entities.RiskAssessmentEntity
        {
            Id = Guid.NewGuid().ToString(),
            AlertId = domain.AlertId,
            CorrelationId = domain.CorrelationId,
            NormalizedSeverity = (int)domain.NormalizedSeverity,
            RiskScore = domain.RiskScore,
            ConfidenceScore = domain.ConfidenceScore,
            RequiredApprovalLevel = domain.RequiredApprovalLevel,
            RiskFactorsJson = System.Text.Json.JsonSerializer.Serialize(domain.RiskFactors),
            Summary = domain.Summary,
            AssessedAt = DateTimeOffset.UtcNow
        };
    }

    private static Entities.AuditEventEntity MapDomainToAuditEntity(AuditEvent domain)
    {
        return new Entities.AuditEventEntity
        {
            Id = domain.Id,
            CorrelationId = domain.CorrelationId,
            EventType = domain.EventType,
            Summary = domain.EventType,
            Details = domain.Details,
            IsSecurityRelevant = domain.IsSecurityRelevant,
            Timestamp = domain.Timestamp,
            Actor = domain.Actor,
            Level = domain.IsSecurityRelevant ? "Warning" : "Info"
        };
    }

    private static RiskAssessment MapEntityToModel(Entities.RiskAssessmentEntity entity)
    {
        var riskFactors = new List<string>();
        if (!string.IsNullOrEmpty(entity.RiskFactorsJson))
        {
            try
            {
                riskFactors = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.RiskFactorsJson) ?? [];
            }
            catch
            {
                riskFactors = [];
            }
        }

        return new RiskAssessment
        {
            Id = entity.Id,
            AlertId = entity.AlertId,
            CorrelationId = entity.CorrelationId,
            NormalizedSeverity = (Severity)entity.NormalizedSeverity,
            RiskScore = entity.RiskScore,
            ConfidenceScore = entity.ConfidenceScore,
            RequiredApprovalLevel = entity.RequiredApprovalLevel,
            RiskFactors = riskFactors,
            Summary = entity.Summary,
            AssessedAt = entity.AssessedAt
        };
    }
}
