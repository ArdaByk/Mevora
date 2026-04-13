using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

public class LifecycleRequest : IRequest<string> { }

// State tutan (CallCount) Transient handler
public class LifecycleHandler : IRequestProcessorAsync<LifecycleRequest, string>
{
    private static int _instanceCounter;
    public readonly int InstanceId;

    public LifecycleHandler()
    {
        InstanceId = Interlocked.Increment(ref _instanceCounter);
    }

    public Task<string> ProcessAsync(LifecycleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Instance-{InstanceId}");
    }
}

public class DependencyLifecycleTests
{
    [Fact]
    public async Task Handlers_Should_Respect_ServiceLifetime_Transient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Mevora'yı Transient handler config'i ile yüklüyoruz
        services.AddMevora(cfg =>
        {
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly);
            cfg.WithServiceLifetime(ServiceLifetime.Scoped);
        });

        // Test handler'ı manuel olarak ekleyelim
        services.AddScoped<IRequestProcessorAsync<LifecycleRequest, string>, LifecycleHandler>();
        services.AddMevoraDispatcher();

        var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        // İlk request (ayrı scope)
        using var scope1 = sp.CreateScope();
        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IMevoraDispatcher>();
        var result1 = await dispatcher1.DispatchAsync(new LifecycleRequest());

        // İkinci request (aynı scope) -> result2 == result1 olmalı
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IMevoraDispatcher>();
        var result2 = await dispatcher2.DispatchAsync(new LifecycleRequest());
        result1.Should().Be(result2, "Scoped within same scope must be identical");

        // Üçüncü request (farklı scope) -> result3 != result1 olmalı
        using var scope3 = sp.CreateScope();
        var dispatcher3 = scope3.ServiceProvider.GetRequiredService<IMevoraDispatcher>();
        var result3 = await dispatcher3.DispatchAsync(new LifecycleRequest());

        result1.Should().NotBe(result3, "Scoped within different scope must be different, static DI leakage detected!");
    }
}
