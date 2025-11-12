using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RedDog.LoyaltyService.Configuration;
using RedDog.LoyaltyService.HealthChecks;
using RedDog.LoyaltyService.Models;
using RedDog.LoyaltyService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
EnsureDaprEnvironmentVariables(builder.Configuration, builder.Environment);

builder.Services.AddOptions<DaprOptions>()
    .BindConfiguration(DaprOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var serviceName = "LoyaltyService";
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

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
});

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

builder.Services.AddDaprClient();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILoyaltyStateService, LoyaltyStateService>();
builder.Services.AddControllers().AddDapr();
builder.Services.AddCors();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy("Service is alive"), tags: new[] { "live" })
    .AddCheck<DaprSidecarHealthCheck>("dapr-readiness", tags: new[] { "ready" });

var app = builder.Build();

var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
var daprOptions = app.Services.GetRequiredService<IOptions<DaprOptions>>().Value;

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(policy =>
    policy.WithOrigins(corsOptions.AllowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());

app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapPost("/dapr/loyalty/orders", async (
        OrderSummary orderSummary,
        ILoyaltyStateService loyaltyStateService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        if (orderSummary is null)
        {
            return Results.BadRequest("OrderSummary cannot be null.");
        }

        var logger = loggerFactory.CreateLogger("LoyaltyService.TopicSubscription");
        logger.LogInformation("Processing loyalty update for customer {LoyaltyId}", orderSummary.LoyaltyId);

        try
        {
            var summary = await loyaltyStateService.UpdateAsync(orderSummary, cancellationToken);
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update loyalty for {LoyaltyId}", orderSummary.LoyaltyId);
            return Results.Problem(ex.Message);
        }
    })
    .WithTopic(daprOptions.PubSubName, daprOptions.OrderTopic);

app.MapControllers();

app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/livez", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

static void EnsureDaprEnvironmentVariables(IConfiguration configuration, IHostEnvironment environment)
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")))
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
