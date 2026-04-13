using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// Event/Message Publisher testleri:
/// Aynı IMessage tipine kayıtlı her Handler ayrı ayrı tetiklenmeli.
/// </summary>
public class EventPublisherTests
{
    private static IMevoraDispatcher BuildDispatcher()
    {
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        services.AddMevoraDispatcher();
        return services.BuildServiceProvider().GetRequiredService<IMevoraDispatcher>();
    }

    [Fact]
    public async Task PublishAsync_Should_Trigger_AllHandlers()
    {
        // Arrange — iki farklı handler var: HandlerA ve HandlerB
        OrderCreatedHandlerA.CallCount = 0;
        OrderCreatedHandlerB.CallCount = 0;
        var dispatcher = BuildDispatcher();

        // Act
        await dispatcher.PublishAsync(new OrderCreatedMessage { OrderId = 42 });

        // Assert — her iki handler da tetiklenmeli
        OrderCreatedHandlerA.CallCount.Should().Be(1, "HandlerA tetiklenmeli");
        OrderCreatedHandlerB.CallCount.Should().Be(1, "HandlerB tetiklenmeli");
    }

    [Fact]
    public async Task PublishAsync_Multiple_Times_Should_Trigger_Handlers_Each_Time()
    {
        // Arrange
        OrderCreatedHandlerA.CallCount = 0;
        OrderCreatedHandlerB.CallCount = 0;
        var dispatcher = BuildDispatcher();

        // Act — 3 kez yayınla
        await dispatcher.PublishAsync(new OrderCreatedMessage { OrderId = 1 });
        await dispatcher.PublishAsync(new OrderCreatedMessage { OrderId = 2 });
        await dispatcher.PublishAsync(new OrderCreatedMessage { OrderId = 3 });

        // Assert — her biri 3 kez çalışmalı
        OrderCreatedHandlerA.CallCount.Should().Be(3);
        OrderCreatedHandlerB.CallCount.Should().Be(3);
    }
}
