using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mevora;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mevora.UnitTests;

public class TestReq2 : IRequest<string> {}
public class MyPipe3<TReq, TRes> : IPipelineAction<TReq, TRes> where TReq : IRequest {
    public async Task<TRes> Run(TReq request, ProcessorDelegate<TRes> next, CancellationToken ct) => await next();
}

public class OpenGenericDITests {
    [Fact]
    public void Test_OpenGeneric() {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IPipelineAction<,>), typeof(MyPipe3<,>));

        var sp = services.BuildServiceProvider();
        var pipes = sp.GetServices<IPipelineAction<TestReq2, string>>();
        pipes.Should().HaveCount(1);
    }
}
