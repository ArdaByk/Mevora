using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq5 : IRequest {}
public class TestReq5Handler : IRequestProcessorAsync<TestReq5> {
    public Task ProcessAsync(TestReq5 request, CancellationToken ct) => Task.CompletedTask;
}

public class MyPipe21<TReq> : IPipelineAction<TReq> where TReq : IRequest {
    public async Task Run(TReq request, ProcessorDelegate next, CancellationToken ct) {
        await next();
    }
}

public class MyPipe22<TReq> : IPipelineAction<TReq> where TReq : IRequest {
    public async Task Run(TReq request, ProcessorDelegate next, CancellationToken ct) {
        await next();
    }
}

public class TryAddPipelineTests3 {
    [Fact]
    public void Test_AddMultiplePipelines3() {
        var services = new ServiceCollection();
        services.AddMevora(cfg => {
            cfg.AddProcessorsFromAssembly(typeof(TryAddPipelineTests3).Assembly);
            cfg.AddPipelineAction(typeof(MyPipe21<>));
            cfg.AddPipelineAction(typeof(MyPipe22<>));
        });

        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq5>>();
        pipes.Should().HaveCount(2);
    }
}
