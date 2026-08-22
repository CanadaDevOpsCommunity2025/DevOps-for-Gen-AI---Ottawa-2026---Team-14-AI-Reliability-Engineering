namespace SecureFix.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SecureFix.Core.Data;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;
using SecureFix.Core.Services;
using Xunit;

/// <summary>
/// Integration tests for alert ingestion service.
/// Tests the complete workflow: validation, deduplication, risk assessment, persistence.
/// </summary>
public class AlertIngestionServiceTests : IAsyncLifetime
{
    private SecureFixDbContext _dbContext = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IRiskScoringEngine _riskScoringEngine = null!;
    private IAlertIngestionService _ingestionService = null!;
    private Mock<ILogger<AlertIngestionService>> _loggerMock = null!;

    public async Task InitializeAsync()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SecureFixDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SecureFixDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Initialize repositories and unit of work
        _unitOfWork = new UnitOfWork(_dbContext);

        // Initialize risk scoring engine with default policy
        var defaultPolicy = new RiskScoringPolicy();
        _riskScoringEngine = new RiskScoringEngine(defaultPolicy);

        // Initialize logger mock
        _loggerMock = new Mock<ILogger<AlertIngestionService>>();

        // Initialize service
        _ingestionService = new AlertIngestionService(
            _unitOfWork,
            _riskScoringEngine,
            _loggerMock.Object);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task IngestAlert_WithValidRequest_ReturnsAcceptedResponse()
    {
        // Arrange
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = "github-12345",
            CveId = "CVE-2024-1234",
            PackageName = "log4j",
            InstalledVersion = "2.14.1",
            FixedVersion = "2.17.0",
            ProviderSeverity = "critical",
            Description = "Critical vulnerability in log4j",
            IsDirectDependency = true,
            IsExploitable = true,
            RepositoryIdentifier = "org/repo"
        };

        // Act
        var response = await _ingestionService.IngestAlertAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsAccepted);
        Assert.NotEmpty(response.WorkflowId);
        Assert.NotEmpty(response.CorrelationId);
        Assert.NotNull(response.Assessment);
        Assert.Equal("Awaiting AI recommendation", response.NextStep);
        Assert.True(response.Assessment.RiskScore > 0);
    }

    [Fact]
    public async Task IngestAlert_CriticalVulnerability_HighRiskScore()
    {
        // Arrange
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = "github-critical-001",
            CveId = "CVE-2024-9999",
            PackageName = "log4j",
            InstalledVersion = "2.14.1",
            FixedVersion = "2.17.0",
            ProviderSeverity = "critical",
            Description = "RCE vulnerability",
            IsDirectDependency = true,
            IsExploitable = true
        };

        // Act
        var response = await _ingestionService.IngestAlertAsync(request);

        // Assert
        Assert.True(response.IsAccepted);
        // Critical = 80 base + 15 direct + 20 exploitable + 10 no fix = 125, clamped to 100
        Assert.Equal(100, response.Assessment.RiskScore);
    }

    [Fact]
    public async Task IngestAlert_LowSeverityIndirectDependency_LowRiskScore()
    {
        // Arrange
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = "github-low-001",
            CveId = "CVE-2024-0001",
            PackageName = "some-package",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.0.1",
            ProviderSeverity = "low",
            Description = "Low severity issue",
            IsDirectDependency = false,
            IsExploitable = false
        };

        // Act
        var response = await _ingestionService.IngestAlertAsync(request);

        // Assert
        Assert.True(response.IsAccepted);
        // Low = 20, no bonuses, fixed version available = 20
        Assert.Equal(20, response.Assessment.RiskScore);
    }

    [Fact]
    public async Task IngestAlert_DuplicateAlert_ReturnsDuplicateResponse()
    {
        // Arrange
        var externalId = "github-dup-001";
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = externalId,
            CveId = "CVE-2024-1234",
            PackageName = "log4j",
            InstalledVersion = "2.14.1",
            FixedVersion = "2.17.0",
            ProviderSeverity = "high",
            Description = "Test vulnerability"
        };

        // First ingestion
        var firstResponse = await _ingestionService.IngestAlertAsync(request);
        Assert.True(firstResponse.IsAccepted);

        // Act - Second ingestion with same external ID
        var secondResponse = await _ingestionService.IngestAlertAsync(request);

        // Assert
        Assert.False(secondResponse.IsAccepted);
        Assert.NotNull(secondResponse.Message);
        Assert.Contains("already ingested", secondResponse.Message.ToLower());
        Assert.Equal(firstResponse.WorkflowId, secondResponse.WorkflowId);
        Assert.Equal(firstResponse.CorrelationId, secondResponse.CorrelationId);
    }

    [Fact]
    public async Task IngestAlert_PersistedToDatabase()
    {
        // Arrange
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = "github-persist-001",
            CveId = "CVE-2024-5678",
            PackageName = "spring-core",
            InstalledVersion = "5.3.0",
            FixedVersion = "5.3.27",
            ProviderSeverity = "high",
            Description = "Persistence test"
        };

        // Act
        var response = await _ingestionService.IngestAlertAsync(request);

        // Assert - Verify data persisted
        var persistedAlert = await _unitOfWork.VulnerabilityAlerts
            .GetByExternalIdAsync("github-persist-001");

        Assert.NotNull(persistedAlert);
        Assert.Equal(request.PackageName, persistedAlert!.PackageName);
        Assert.Equal(request.CveId, persistedAlert.CveId);

        // Verify risk assessment persisted
        var persistedAssessment = await _unitOfWork.RiskAssessments
            .GetByAlertIdAsync(persistedAlert.Id);

        Assert.NotNull(persistedAssessment);
        if (persistedAssessment != null)
        {
            Assert.Equal(response.Assessment.RiskScore, persistedAssessment.RiskScore);
        }
    }

    [Fact]
    public async Task IsDuplicate_ExistingAlert_ReturnsTrue()
    {
        // Arrange
        var request = new AlertIngestionRequest
        {
            ExternalAlertId = "github-check-001",
            CveId = "CVE-2024-1111",
            PackageName = "test-pkg",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.1.0",
            ProviderSeverity = "medium",
            Description = "Test"
        };

        await _ingestionService.IngestAlertAsync(request);

        // Act
        var isDuplicate = await _ingestionService.IsDuplicateAsync("github-check-001");

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicate_NonExistentAlert_ReturnsFalse()
    {
        // Act
        var isDuplicate = await _ingestionService.IsDuplicateAsync("github-nonexistent-999");

        // Assert
        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task IngestAlert_MultipleAlerts_EachHasUniqueWorkflowId()
    {
        // Arrange & Act
        var response1 = await _ingestionService.IngestAlertAsync(new AlertIngestionRequest
        {
            ExternalAlertId = "github-multi-001",
            PackageName = "pkg1",
            InstalledVersion = "1.0.0",
            FixedVersion = "1.1.0",
            ProviderSeverity = "high",
            Description = "Test"
        });

        var response2 = await _ingestionService.IngestAlertAsync(new AlertIngestionRequest
        {
            ExternalAlertId = "github-multi-002",
            PackageName = "pkg2",
            InstalledVersion = "2.0.0",
            FixedVersion = "2.1.0",
            ProviderSeverity = "medium",
            Description = "Test"
        });

        // Assert
        Assert.NotEqual(response1.WorkflowId, response2.WorkflowId);
        Assert.NotEqual(response1.CorrelationId, response2.CorrelationId);
        Assert.True(response1.IsAccepted);
        Assert.True(response2.IsAccepted);
    }
}
