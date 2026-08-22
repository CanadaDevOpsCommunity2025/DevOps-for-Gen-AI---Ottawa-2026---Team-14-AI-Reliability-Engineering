namespace SecureFix.Core.Data;

using Microsoft.EntityFrameworkCore;
using SecureFix.Core.Entities;

/// <summary>
/// Entity Framework Core DbContext for SecureFix persistence.
/// Designed to work with SQLite for development/demo and PostgreSQL for production.
/// No dialect-specific code - uses standard EF Core APIs.
/// </summary>
public class SecureFixDbContext : DbContext
{
    public SecureFixDbContext(DbContextOptions<SecureFixDbContext> options)
        : base(options)
    {
    }

    public DbSet<VulnerabilityAlertEntity> VulnerabilityAlerts { get; set; } = null!;
    public DbSet<RiskAssessmentEntity> RiskAssessments { get; set; } = null!;
    public DbSet<RemediationRecommendationEntity> RemediationRecommendations { get; set; } = null!;
    public DbSet<ApprovalDecisionEntity> ApprovalDecisions { get; set; } = null!;
    public DbSet<AuditEventEntity> AuditEvents { get; set; } = null!;
    public DbSet<PullRequestProposalEntity> PullRequestProposals { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // VulnerabilityAlert configuration
        modelBuilder.Entity<VulnerabilityAlertEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ExternalAlertId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CveId).HasMaxLength(50);
            entity.Property(e => e.PackageName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.InstalledVersion).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FixedVersion).HasMaxLength(100);
            entity.Property(e => e.ProviderSeverity).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.RepositoryIdentifier).HasMaxLength(500);
            entity.Property(e => e.AdvisoryUrl).HasMaxLength(2000);

            // Index for deduplication by ExternalAlertId
            entity.HasIndex(e => e.ExternalAlertId).IsUnique();
            // Index for correlation tracing
            entity.HasIndex(e => e.CorrelationId);

            // Relationships
            entity.HasMany(e => e.RiskAssessments)
                .WithOne(r => r.Alert)
                .HasForeignKey(r => r.AlertId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Recommendations)
                .WithOne(r => r.Alert)
                .HasForeignKey(r => r.AlertId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditEvents)
                .WithOne(a => a.Alert)
                .HasForeignKey(a => a.AlertId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // RiskAssessment configuration
        modelBuilder.Entity<RiskAssessmentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.AlertId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequiredApprovalLevel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.RiskFactorsJson).HasColumnType("TEXT");

            // Indexes for queries
            entity.HasIndex(e => e.AlertId);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.NormalizedSeverity);
            entity.HasIndex(e => e.RiskScore);
        });

        // RemediationRecommendation configuration
        modelBuilder.Entity<RemediationRecommendationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.AlertId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RiskAssessmentId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RecommendedAction).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TargetVersion).HasMaxLength(100);
            entity.Property(e => e.Explanation).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Assumptions).HasMaxLength(1000);
            entity.Property(e => e.ModelIdentifier).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PromptVersion).HasMaxLength(100);
            entity.Property(e => e.PotentialRisks).HasMaxLength(1000);
            entity.Property(e => e.AlternativeActionsJson).HasColumnType("TEXT");

            // Indexes
            entity.HasIndex(e => e.AlertId);
            entity.HasIndex(e => e.CorrelationId);
        });

        // ApprovalDecision configuration
        modelBuilder.Entity<ApprovalDecisionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.WorkflowId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AlertId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReviewerIdentity).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReviewerRole).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.Comments).HasMaxLength(2000);
            entity.Property(e => e.ClientIdentifier).HasMaxLength(100);

            // Indexes for queries
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.AlertId);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Status);
        });

        // AuditEvent configuration
        modelBuilder.Entity<AuditEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.WorkflowId).HasMaxLength(500);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Level).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(5000);
            entity.Property(e => e.Actor).HasMaxLength(500);
            entity.Property(e => e.Service).HasMaxLength(100);
            entity.Property(e => e.AlertId).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnType("TEXT");

            // Critical index for correlation tracing
            entity.HasIndex(e => e.CorrelationId);
            // Index for timeline queries
            entity.HasIndex(e => e.Timestamp);
            // Index for event type filtering
            entity.HasIndex(e => e.EventType);
            // Index for security events
            entity.HasIndex(e => e.IsSecurityRelevant);
        });

        // PullRequestProposal configuration
        modelBuilder.Entity<PullRequestProposalEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.RecommendationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AlertId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ProposedTitle).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ProposedDescription).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.FilesForReviewJson).HasColumnType("TEXT");
            entity.Property(e => e.DependencyChangesJson).HasColumnType("TEXT");
            entity.Property(e => e.ValidationCommandsJson).HasColumnType("TEXT");
            entity.Property(e => e.RollbackGuidance).HasMaxLength(1000);
            entity.Property(e => e.KnownLimitations).HasMaxLength(1000);
            entity.Property(e => e.ResourceLinksJson).HasColumnType("TEXT");
            entity.Property(e => e.EstimatedEffort).HasMaxLength(50);
            entity.Property(e => e.RawProposalJson).HasColumnType("TEXT");

            // Indexes for queries
            entity.HasIndex(e => e.RecommendationId);
            entity.HasIndex(e => e.AlertId);
            entity.HasIndex(e => e.CorrelationId);
        });
    }
}
