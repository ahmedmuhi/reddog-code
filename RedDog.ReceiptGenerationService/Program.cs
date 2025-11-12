using Dapr.Client;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RedDog.ReceiptGenerationService.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ADR-0006: Validate required environment variables at startup
var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
    ?? throw new InvalidOperationException("ASPNETCORE_URLS environment variable is required");
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

// ADR-0011: OpenTelemetry observability with OTLP exporter
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
var serviceName = "RedDog.ReceiptGenerationService";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
});

// Add services
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();

// ADR-0005: Kubernetes health probe standardization
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: new[] { "live" })
    .AddCheck<DaprSidecarHealthCheck>("readiness", tags: new[] { "ready" });

var app = builder.Build();

// Development exception page
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Routing
app.UseRouting();

// Dapr CloudEvents middleware (for pub/sub)
app.UseCloudEvents();

// Authorization
app.UseAuthorization();

// Map endpoints
app.MapControllers();

// Dapr subscription handler
app.MapSubscribeHandler();

// ADR-0005: Health probe endpoints
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/livez", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
