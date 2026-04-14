using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq6 : IRequest<string> {}
public class TestReq6Handler : IRequestProcessorAsync<TestReq6, string> {
    public Task<string> ProcessAsync(TestReq6 request, CancellationToken ct) => Task.FromResult("ok");
}

public class TryAddPipelineTests4 {
    [Fact]
    public void Test_AddMultiplePipelines4() {
        var services = new ServiceCollection();
        services.AddMevora(cfg => {
            cfg.AddProcessorsFromAssembly(typeof(TryAddPipelineTests4).Assembly);
            // using the open generic pipeline logic inside PipelineRegistrar
        });

        // Ensure no exception is thrown when fetching from open generic
        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq6, string>>();
        pipes.Should().HaveCount(0);
    }
}
