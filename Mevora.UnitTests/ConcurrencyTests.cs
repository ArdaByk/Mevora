using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

public class DelayCommand : IRequest<string>
{
    public int DelayMs { get; init; }
}

public class DelayCommandHandler : IRequestProcessorAsync<DelayCommand, string>
{
    public async Task<string> ProcessAsync(DelayCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return "Delayed";
    }
}

public class ConcurrencyTests
{
    [Fact]
    public async Task Concurrent_DispatchAsync_ShouldHandle_ParallelExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMevora(cfg => cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        // Add handler
        services.AddTransient<IRequestProcessorAsync<DelayCommand, string>, DelayCommandHandler>();
        services.AddMevoraDispatcher();

        var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetRequiredService<IMevoraDispatcher>();

        // Act: Aynı anda 100 ping (veya delay) command göndereceğiz
        var tasks = Enumerable.Range(0, 100)
                              .Select(_ => dispatcher.DispatchAsync(new DelayCommand { DelayMs = 10 }))
                              .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.All(r => r == "Delayed").Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithCancellation_Should_CancelExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMevora(cfg => cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly)); // PingRequest için
        services.AddTransient<IRequestProcessorAsync<DelayCommand, string>, DelayCommandHandler>();
        services.AddMevoraDispatcher();
        
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IMevoraDispatcher>();

        var cts = new CancellationTokenSource();
        cts.CancelAfter(5); // 5ms sonra iptal!

        // Act
        Func<Task> act = async () => await dispatcher.DispatchAsync(new DelayCommand { DelayMs = 100 }, cts.Token);

        // Assert: Task iptal the edilmelidir
        await act.Should().ThrowAsync<TaskCanceledException>();
    }
}
