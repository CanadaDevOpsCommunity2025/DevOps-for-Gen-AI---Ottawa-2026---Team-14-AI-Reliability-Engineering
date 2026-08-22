namespace SecureFix.Core.Repositories;

using SecureFix.Core.Entities;

/// <summary>
/// Base repository interface for common operations.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
    Task SaveChangesAsync();
}

/// <summary>
/// Repository for VulnerabilityAlert persistence.
/// </summary>
public interface IVulnerabilityAlertRepository : IRepository<VulnerabilityAlertEntity>
{
    Task<VulnerabilityAlertEntity?> GetByExternalIdAsync(string externalAlertId);
    Task<IEnumerable<VulnerabilityAlertEntity>> GetByCorrelationIdAsync(string correlationId);
}

/// <summary>
/// Repository for RiskAssessment persistence.
/// </summary>
public interface IRiskAssessmentRepository : IRepository<RiskAssessmentEntity>
{
    Task<RiskAssessmentEntity?> GetByAlertIdAsync(string alertId);
    Task<IEnumerable<RiskAssessmentEntity>> GetBySeverityAsync(int severity);
    Task<IEnumerable<RiskAssessmentEntity>> GetByRiskScoreRangeAsync(int minScore, int maxScore);
}

/// <summary>
/// Repository for RemediationRecommendation persistence.
/// </summary>
public interface IRemediationRecommendationRepository : IRepository<RemediationRecommendationEntity>
{
    Task<RemediationRecommendationEntity?> GetByAlertIdAsync(string alertId);
    Task<IEnumerable<RemediationRecommendationEntity>> GetByProviderAsync(string modelIdentifier);
    Task<IEnumerable<RemediationRecommendationEntity>> GetByCorrelationIdAsync(string correlationId);
}

/// <summary>
/// Repository for ApprovalDecision persistence.
/// </summary>
public interface IApprovalDecisionRepository : IRepository<ApprovalDecisionEntity>
{
    Task<ApprovalDecisionEntity?> GetByWorkflowIdAsync(string workflowId);
    Task<ApprovalDecisionEntity?> GetByAlertIdAsync(string alertId);
    Task<IEnumerable<ApprovalDecisionEntity>> GetByReviewerAsync(string reviewerIdentity);
    Task<IEnumerable<ApprovalDecisionEntity>> GetByStatusAsync(int status);
}

/// <summary>
/// Repository for AuditEvent persistence.
/// </summary>
public interface IAuditEventRepository : IRepository<AuditEventEntity>
{
    Task<IEnumerable<AuditEventEntity>> GetByCorrelationIdAsync(string correlationId);
    Task<IEnumerable<AuditEventEntity>> GetByEventTypeAsync(string eventType);
    Task<IEnumerable<AuditEventEntity>> GetSecurityEventsAsync();
    Task<IEnumerable<AuditEventEntity>> GetTimelineAsync(DateTimeOffset startTime, DateTimeOffset endTime);
}

/// <summary>
/// Unit of Work pattern for coordinating multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IVulnerabilityAlertRepository VulnerabilityAlerts { get; }
    IRiskAssessmentRepository RiskAssessments { get; }
    IRemediationRecommendationRepository RemediationRecommendations { get; }
    IApprovalDecisionRepository ApprovalDecisions { get; }
    IAuditEventRepository AuditEvents { get; }
    
    Task<int> SaveChangesAsync();
}
