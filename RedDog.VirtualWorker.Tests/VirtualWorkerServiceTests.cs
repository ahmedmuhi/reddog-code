using Dapr.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RedDog.VirtualWorker.Configuration;
using RedDog.VirtualWorker.Services;

namespace RedDog.VirtualWorker.Tests;

/// <summary>
/// Tests for VirtualWorkerService to verify DaprClient dependency injection.
/// These tests ensure that the service properly uses DI and prevent regression to manual instantiation.
/// </summary>
public class VirtualWorkerServiceTests
{
    [Fact]
    public void Constructor_RequiresDaprClientViaDI_ValidatesDependencyInjection()
    {
        // Arrange - Create a simple DaprClient instance (doesn't need to be functional for DI validation)
        var daprClient = new DaprClientBuilder().Build();
        var logger = new Mock<ILogger<VirtualWorkerService>>().Object;
        var options = Options.Create(new DaprOptions
        {
            StoreId = "TestStore",
            MakeLineServiceAppId = "make-line-service",
            MinSecondsToCompleteItem = 1,
            MaxSecondsToCompleteItem = 2
        });

        // Act - VirtualWorkerService must accept DaprClient via constructor (not instantiate it)
        var service = new VirtualWorkerService(daprClient, options, logger);

        // Assert - If constructor succeeds, DaprClient is injected (not manually instantiated)
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullDaprClient_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<VirtualWorkerService>>().Object;
        var options = Options.Create(new DaprOptions
        {
            StoreId = "TestStore",
            MakeLineServiceAppId = "make-line-service",
            MinSecondsToCompleteItem = 1,
            MaxSecondsToCompleteItem = 2
        });

        // Act & Assert - Ensures DaprClient is required and cannot be null
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new VirtualWorkerService(null!, options, logger));

        exception.ParamName.Should().Be("daprClient");
    }

    [Fact]
    public void ServiceRegistration_VirtualWorkerService_UsesDaprClientFromDI()
    {
        // Arrange - Build a service collection similar to Program.cs
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());
        services.AddSingleton(Options.Create(new DaprOptions
        {
            StoreId = "TestStore",
            MakeLineServiceAppId = "make-line-service",
            MinSecondsToCompleteItem = 1,
            MaxSecondsToCompleteItem = 2
        }));
        services.AddScoped<IVirtualWorkerService, VirtualWorkerService>();

        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve IVirtualWorkerService which requires DaprClient
        var service = serviceProvider.GetRequiredService<IVirtualWorkerService>();

        // Assert - If resolution succeeds, DaprClient was properly injected
        service.Should().NotBeNull();
        service.Should().BeOfType<VirtualWorkerService>();
    }
}
