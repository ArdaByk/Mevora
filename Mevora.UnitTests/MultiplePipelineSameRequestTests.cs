using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq3 : IRequest {}
public class TestReq3Handler : IRequestProcessorAsync<TestReq3> {
    public Task ProcessAsync(TestReq3 request, CancellationToken ct) => Task.CompletedTask;
}

public class MyPipe4<TReq> : IPipelineAction<TReq> where TReq : IRequest {
    public async Task Run(TReq request, ProcessorDelegate next, CancellationToken ct) => await next();
}

public class MyPipe5<TReq> : IPipelineAction<TReq> where TReq : IRequest {
    public async Task Run(TReq request, ProcessorDelegate next, CancellationToken ct) => await next();
}

public class MultiplePipelineSameRequestTests {
    [Fact]
    public void Test_AddMultiplePipelinesWithoutResponse() {
        var services = new ServiceCollection();
        services.AddMevora(cfg => {
            cfg.AddProcessorsFromAssembly(typeof(MultiplePipelineSameRequestTests).Assembly);
            cfg.AddPipelineAction(typeof(MyPipe4<>));
            cfg.AddPipelineAction(typeof(MyPipe5<>));
        });

        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq3>>();
        pipes.Should().HaveCount(2);
    }
}
