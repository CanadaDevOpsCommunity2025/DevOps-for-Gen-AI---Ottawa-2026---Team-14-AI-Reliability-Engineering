namespace SecureFix.Core.Repositories;

using SecureFix.Core.Data;

/// <summary>
/// Unit of Work implementation coordinating multiple repositories.
/// Ensures consistent transaction handling and single save operation.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SecureFixDbContext _context;
    private IVulnerabilityAlertRepository? _vulnerabilityAlerts;
    private IRiskAssessmentRepository? _riskAssessments;
    private IRemediationRecommendationRepository? _remediationRecommendations;
    private IApprovalDecisionRepository? _approvalDecisions;
    private IAuditEventRepository? _auditEvents;

    public UnitOfWork(SecureFixDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IVulnerabilityAlertRepository VulnerabilityAlerts =>
        _vulnerabilityAlerts ??= new VulnerabilityAlertRepository(_context);

    public IRiskAssessmentRepository RiskAssessments =>
        _riskAssessments ??= new RiskAssessmentRepository(_context);

    public IRemediationRecommendationRepository RemediationRecommendations =>
        _remediationRecommendations ??= new RemediationRecommendationRepository(_context);

    public IApprovalDecisionRepository ApprovalDecisions =>
        _approvalDecisions ??= new ApprovalDecisionRepository(_context);

    public IAuditEventRepository AuditEvents =>
        _auditEvents ??= new AuditEventRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
