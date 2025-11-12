using Dapr.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

// Validate required infrastructure environment variables (ADR-0006)
ValidateEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry (ADR-0011)
var serviceName = "OrderService";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4318";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Dapr.*")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Dapr.*")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

// Configure logging with OpenTelemetry
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
});

// Configure Dapr
builder.Services.AddDaprClient();
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();

// Configure CORS (ADR-0006 - temporary until Dapr Config API implemented per ADR-0004)
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',')
    ?? ["http://localhost:8080"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure OpenAPI + Scalar (Web API Standards)
builder.Services.AddOpenApi();

// Configure Health Checks (ADR-0005)
builder.Services.AddHealthChecks()
    .AddCheck("startup", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service started successfully"),
        tags: ["startup"])
    .AddCheck("live", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: ["live"])
    .AddAsyncCheck("ready", async () =>
    {
        // Check Dapr sidecar health
        try
        {
            var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://localhost:{daprPort}/v1.0/healthz");

            if (!response.IsSuccessStatusCode)
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Dapr sidecar not healthy");

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Dapr sidecar is healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Dapr sidecar unreachable", ex);
        }
    }, tags: ["ready"]);

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseCloudEvents();
app.MapSubscribeHandler();

// Health endpoints (ADR-0005)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("startup")
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

// Environment variable validation (ADR-0006)
static void ValidateEnvironmentVariables()
{
    var required = new[] { "ASPNETCORE_URLS", "DAPR_HTTP_PORT" };
    var missing = required.Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))).ToList();

    if (missing.Count > 0)
        throw new InvalidOperationException(
            $"Missing required environment variables: {string.Join(", ", missing)}");
}
