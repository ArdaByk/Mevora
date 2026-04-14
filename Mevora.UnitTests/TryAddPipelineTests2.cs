using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq4 : IRequest<string> {}
public class TestReq4Handler : IRequestProcessorAsync<TestReq4, string> {
    public Task<string> ProcessAsync(TestReq4 request, CancellationToken ct) => Task.FromResult("ok");
}

public class MyPipe11<TReq, TRes> : IPipelineAction<TReq, TRes> where TReq : IRequest {
    public async Task<TRes> Run(TReq request, ProcessorDelegate<TRes> next, CancellationToken ct) {
        return await next();
    }
}

public class MyPipe12<TReq, TRes> : IPipelineAction<TReq, TRes> where TReq : IRequest {
    public async Task<TRes> Run(TReq request, ProcessorDelegate<TRes> next, CancellationToken ct) {
        return await next();
    }
}

public class TryAddPipelineTests2 {
    [Fact]
    public void Test_AddMultiplePipelines2() {
        var services = new ServiceCollection();
        services.AddMevora(cfg => {
            cfg.AddProcessorsFromAssembly(typeof(TryAddPipelineTests2).Assembly);
            cfg.AddPipelineAction(typeof(MyPipe11<,>));
            cfg.AddPipelineAction(typeof(MyPipe12<,>));
        });

        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq4, string>>();
        pipes.Should().HaveCount(2);
    }
}
