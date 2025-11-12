using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RedDog.AccountingModel;
using RedDog.AccountingService.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Service name for OpenTelemetry (ADR-0011)
const string serviceName = "accountingservice";
const string serviceVersion = "1.0.0";

// Get connection string from configuration (ADR-0006)
var connectionString = builder.Configuration["ConnectionStrings:RedDog"]
    ?? throw new InvalidOperationException("Database connection string 'ConnectionStrings:RedDog' not found in configuration");

// Handle ${SA_PASSWORD} substitution in connection string
var saPassword = builder.Configuration["SA_PASSWORD"];
if (!string.IsNullOrEmpty(saPassword) && connectionString.Contains("${SA_PASSWORD}"))
{
    connectionString = connectionString.Replace("${SA_PASSWORD}", saPassword);
}

// Add services
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 10 built-in OpenAPI

// DbContext with compiled model and retry logic
builder.Services.AddDbContext<AccountingContext>(options =>
{
    options.UseModel(RedDog.AccountingModel.AccountingContextModel.Instance);
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});

// Health checks (ADR-0005)
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: ["live"])
    .AddCheck<DaprSidecarHealthCheck>("dapr-readiness", tags: ["ready"])
    .AddDbContextCheck<AccountingContext>("database-readiness", tags: ["ready"]);

// CORS (ADR-0011: Web API Standards)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// OpenTelemetry (ADR-0011)
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion: serviceVersion));
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseCloudEvents();
app.MapSubscribeHandler();

// OpenAPI/Scalar (always enabled per ADR)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Accounting Service API");
});

// Health check endpoints (ADR-0005)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/livez", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
