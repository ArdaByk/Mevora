using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Moq;
using Mevora;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using testAPI.queries;

namespace BenchmarkApp;

[MemoryDiagnoser] // Bellek kullanımını da ölçer
public class DispatcherBenchmark
{
    private IMevoraDispatcher _mevoraDispatcher;
    private IMediator _mediatr;

    private MevoraGetAllDataQuery _mevoraQuery;
    private MediatrGetAllDataQuery _mediatrQuery;

    [GlobalSetup]
    public void Setup()
    {
        // Mock Mevora Dispatcher
        var mevoraMock = new Mock<IMevoraDispatcher>();
        mevoraMock
            .Setup(m => m.DispatchAsync(It.IsAny<MevoraGetAllDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new string[] { "Test1", "Test2" });
        _mevoraDispatcher = mevoraMock.Object;

        // Mock Mediatr
        var mediatrMock = new Mock<IMediator>();
        mediatrMock
            .Setup(m => m.Send(It.IsAny<MediatrGetAllDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new string[] { "Test1", "Test2" });
        _mediatr = mediatrMock.Object;

        // Örnek query objeleri
        _mevoraQuery = new MevoraGetAllDataQuery();
        _mediatrQuery = new MediatrGetAllDataQuery();
    }

    [Benchmark]
    public async Task<string[]> MevoraDispatchAsync()
    {
        return await _mevoraDispatcher.DispatchAsync(_mevoraQuery);
    }

    [Benchmark]
    public async Task<string[]> MediatrSendAsync()
    {
        return await _mediatr.Send(_mediatrQuery);
    }
}
