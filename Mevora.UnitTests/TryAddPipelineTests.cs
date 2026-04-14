using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq : IRequest<string> {}
public class TestReqHandler : IRequestProcessorAsync<TestReq, string> {
    public Task<string> ProcessAsync(TestReq request, CancellationToken ct) => Task.FromResult("ok");
}

public class MyPipe1<TReq, TRes> : IPipelineAction<TReq, TRes> where TReq : IRequest {
    public async Task<TRes> Run(TReq request, ProcessorDelegate<TRes> next, CancellationToken ct) {
        return await next();
    }
}

public class MyPipe2<TReq, TRes> : IPipelineAction<TReq, TRes> where TReq : IRequest {
    public async Task<TRes> Run(TReq request, ProcessorDelegate<TRes> next, CancellationToken ct) {
        return await next();
    }
}

public class TryAddPipelineTests {
    [Fact]
    public void Test_AddMultiplePipelines() {
        var services = new ServiceCollection();
        services.AddMevora(cfg => {
            cfg.AddProcessorsFromAssembly(typeof(TryAddPipelineTests).Assembly);
            cfg.AddPipelineAction(typeof(MyPipe1<,>));
            cfg.AddPipelineAction(typeof(MyPipe2<,>));
        });

        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq, string>>();
        pipes.Should().HaveCount(2);
    }
}
