using Dapr.Client;
using Dapr.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Extensions.Hosting;
using RedDog.VirtualCustomers.Configuration;
using RedDog.VirtualCustomers;

var builder = Host.CreateApplicationBuilder(args);

// Load business config from Dapr Configuration API (ADR-0004).
// Separate DaprClient is needed because the DI container isn't built yet.
var configClient = new DaprClientBuilder().Build();
builder.Configuration.AddDaprConfigurationStore(
    "reddog.config",
    new List<string>
    {
        "VirtualCustomers||StoreId",
        "VirtualCustomers||MaxItemQuantity",
        "VirtualCustomers||MaxUniqueItemsPerOrder",
        "VirtualCustomers||MinSecondsToPlaceOrder",
        "VirtualCustomers||MaxSecondsToPlaceOrder",
        "VirtualCustomers||MinSecondsBetweenOrders",
        "VirtualCustomers||MaxSecondsBetweenOrders",
    },
    configClient,
    TimeSpan.FromSeconds(60));

builder.Services.AddOptions<VirtualCustomerOptions>()
    .BindConfiguration(VirtualCustomerOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(opts => opts.MinSecondsToPlaceOrder <= opts.MaxSecondsToPlaceOrder, "MinSecondsToPlaceOrder must be <= MaxSecondsToPlaceOrder")
    .Validate(opts => opts.MinSecondsBetweenOrders <= opts.MaxSecondsBetweenOrders, "MinSecondsBetweenOrders must be <= MaxSecondsBetweenOrders");

var serviceName = "VirtualCustomers";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4318";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddMeter(serviceName)
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

builder.Services.AddHttpClient();
builder.Services.AddDaprClient();
builder.Services.AddHostedService<VirtualCustomersWorker>();

await builder.Build().RunAsync();
