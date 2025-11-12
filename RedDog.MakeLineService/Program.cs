using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RedDog.MakeLineService.Configuration;
using RedDog.MakeLineService.HealthChecks;
using RedDog.MakeLineService.Models;
using RedDog.MakeLineService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Ensure required infrastructure environment variables (ADR-0006)
EnsureDaprEnvironmentVariables(builder.Configuration, builder.Environment);

// Configure strongly-typed options with validation (C# Pro pattern)
builder.Services.AddOptions<DaprOptions>()
    .BindConfiguration(DaprOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure OpenTelemetry (ADR-0011)
var serviceName = "MakeLineService";
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

// Add console logging for local development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

// Configure Dapr
builder.Services.AddDaprClient();
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();
builder.Services.AddScoped<IMakelineQueueProcessor, MakelineQueueProcessor>();

// Configure CORS (ADR-0006 - temporary until Dapr Config API per ADR-0004)
builder.Services.AddCors();

// Configure OpenAPI + Scalar (Web API Standards)
builder.Services.AddOpenApi();

// Configure Health Checks (ADR-0005)
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: ["live"])
    .AddCheck<DaprSidecarHealthCheck>("dapr-readiness", tags: ["ready"]);

var app = builder.Build();

var corsSettings = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
var daprOptions = app.Services.GetRequiredService<IOptions<DaprOptions>>().Value;

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(policy =>
    policy.WithOrigins(corsSettings.AllowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapPost("/dapr/makeline/orders", async (
        OrderSummary orderSummary,
        IMakelineQueueProcessor processor,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        if (orderSummary == null)
        {
            return Results.BadRequest("OrderSummary cannot be null");
        }

        var logger = loggerFactory.CreateLogger("MakeLineService.TopicSubscription");
        logger.LogInformation("Received order {OrderId} for store {StoreId}", orderSummary.OrderId, orderSummary.StoreId);

        try
        {
            await processor.AddOrderAsync(orderSummary, cancellationToken);
            logger.LogInformation("Successfully processed order {OrderId}", orderSummary.OrderId);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing order {OrderId}", orderSummary.OrderId);
            return Results.Problem(ex.Message);
        }
    })
    .WithTopic(daprOptions.PubSubName, daprOptions.OrderTopic);

// Health endpoints (ADR-0005)
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

// Environment variable validation (ADR-0006)
static void EnsureDaprEnvironmentVariables(IConfiguration configuration, IHostEnvironment environment)
{
    var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
    if (!string.IsNullOrWhiteSpace(daprHttpPort))
    {
        return;
    }

    var configuredPort = configuration.GetValue<string?>("Dapr:HttpPort");
    if (!string.IsNullOrWhiteSpace(configuredPort))
    {
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", configuredPort);
    }
    else if (environment.IsDevelopment())
    {
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "3500");
    }

    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")))
    {
        throw new InvalidOperationException("Missing required environment variable: DAPR_HTTP_PORT");
    }
}
