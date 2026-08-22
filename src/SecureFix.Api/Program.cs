using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;
using SecureFix.Core.Data;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;
using SecureFix.Core.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddOpenApi();
    builder.Services.AddControllers();

    // Configure database
    var dbPath = builder.Configuration["Database:Path"] ?? "securefix.db";
    builder.Services.AddDbContext<SecureFixDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Register repositories and unit of work
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

    // Register repository implementations
    builder.Services.AddScoped<IVulnerabilityAlertRepository>(sp =>
        new VulnerabilityAlertRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IRiskAssessmentRepository>(sp =>
        new RiskAssessmentRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IRemediationRecommendationRepository>(sp =>
        new RemediationRecommendationRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IApprovalDecisionRepository>(sp =>
        new ApprovalDecisionRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IAuditEventRepository>(sp =>
        new AuditEventRepository(sp.GetRequiredService<SecureFixDbContext>()));

    // Register business services
    builder.Services.AddScoped<IRiskScoringEngine, RiskScoringEngine>();
    builder.Services.AddScoped<IAlertIngestionService, AlertIngestionService>();
    builder.Services.AddScoped<IApprovalService, ApprovalService>();

    // Register AI provider factory and provider
    builder.Services.AddScoped<IAIRecommendationProviderFactory, AIRecommendationProviderFactory>();
    builder.Services.AddScoped<IAIRecommendationProvider>(sp =>
    {
        var factory = sp.GetRequiredService<IAIRecommendationProviderFactory>();
        return factory.GetProvider();
    });

    // Register validators
    builder.Services.AddScoped<IValidator<AlertIngestionRequest>, AlertIngestionRequestValidator>();

    // Add CORS (for demo purposes)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("Health");

    // Readiness check endpoint
    app.MapGet("/ready", async (SecureFixDbContext db) =>
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.StatusCode(503);
        }
    })
    .WithName("Ready");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

