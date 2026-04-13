using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;

namespace Mevora.UnitTests;

/// <summary>
/// Pipeline çalışma davranışı testleri.
/// Matruşka / Onion mimarisi sırasının doğrulanması.
/// </summary>
public class PipelineExecutionTests
{
    // ────────────────────────────────────────────
    //  Yardımcı — tek pipeline'lı dispatcher
    // ────────────────────────────────────────────
    private static IMevoraDispatcher BuildWithPipelines(List<string> tracker, params Type[] pipelineTypes)
    {
        var services = new ServiceCollection();

        services.AddMevora(cfg =>
        {
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly);
            foreach (var t in pipelineTypes)
                cfg.AddPipelineAction(t);
        });

        // TrackingPipeline'lar tracker listesine ihtiyaç duyar; elle kayıt
        services.AddTransient<IPipelineAction<PingRequest, PingResponse>>(
            _ => new TrackingPipelineA<PingRequest, PingResponse>(tracker));

        if (pipelineTypes.Length > 1)
            services.AddTransient<IPipelineAction<PingRequest, PingResponse>>(
                _ => new TrackingPipelineB<PingRequest, PingResponse>(tracker));

        services.AddMevoraDispatcher();

        return services.BuildServiceProvider().GetRequiredService<IMevoraDispatcher>();
    }

    // ────────────────────────────────────────────
    //  Testler
    // ────────────────────────────────────────────

    [Fact]
    public async Task SinglePipeline_Should_Execute()
    {
        // Arrange
        var tracker = new List<string>();
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));
        services.AddTransient<IPipelineAction<PingRequest, PingResponse>>(
            _ => new TrackingPipelineA<PingRequest, PingResponse>(tracker));
        services.AddMevoraDispatcher();

        var dispatcher = services.BuildServiceProvider()
                                  .GetRequiredService<IMevoraDispatcher>();

        // Act
        var response = await dispatcher.DispatchAsync(new PingRequest());

        // Assert — pipeline devreye girdi, ardından handler çalıştı
        response.Should().NotBeNull();
        tracker.Should().ContainInOrder("A_In", "A_Out");
    }

    [Fact]
    public async Task MultiplePipelines_Should_ExecuteInCorrectOrder()
    {
        // Arrange ─ iki pipeline: A ve B
        var tracker = new List<string>();
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));

        // DI sırası: A önce, B sonra → beklenen çalışma: A_In → B_In → Handler → B_Out → A_Out
        services.AddTransient<IPipelineAction<PingRequest, PingResponse>>(
            _ => new TrackingPipelineA<PingRequest, PingResponse>(tracker));
        services.AddTransient<IPipelineAction<PingRequest, PingResponse>>(
            _ => new TrackingPipelineB<PingRequest, PingResponse>(tracker));
        services.AddMevoraDispatcher();

        var dispatcher = services.BuildServiceProvider()
                                  .GetRequiredService<IMevoraDispatcher>();

        // Act
        await dispatcher.DispatchAsync(new PingRequest());

        // Assert — Matruşka / Onion mimarisi sırası doğrulanıyor
        tracker.Should().ContainInOrder("A_In", "B_In", "B_Out", "A_Out");
    }
}
