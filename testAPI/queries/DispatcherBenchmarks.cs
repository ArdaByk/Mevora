using BenchmarkDotNet.Attributes;
using MediatR;
using Mevora;
using Moq;
using System.Net;

namespace testAPI.queries;


[MemoryDiagnoser]
public class DispatcherBenchmarks
{
    private IMevoraDispatcher _mevoraDispatcher;
    private IMediator _mediatr;
    private MevoraGetAllDataQuery _mevoraQuery;
    private MediatrGetAllDataQuery _mediatrQuery;

    [GlobalSetup]
    public void Setup()
    {
        // MevoraDispatcher için mock
        var mockServiceProvider = new Mock<IServiceProvider>();

        var mockProcessor = new Mock<IRequestProcessorAsync<MevoraGetAllDataQuery, string[]>>();
        mockProcessor
            .Setup(x => x.ProcessAsync(It.IsAny<MevoraGetAllDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new string[] { "data1", "data2", "data3" });

        mockServiceProvider
            .Setup(x => x.GetService(typeof(IRequestProcessorAsync<MevoraGetAllDataQuery, string[]>)))
            .Returns(mockProcessor.Object);

        _mevoraDispatcher = new MevoraDispatcher(mockServiceProvider.Object);

        // Mediatr için mock
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(x => x.Send(It.IsAny<MediatrGetAllDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new string[] { "data1", "data2", "data3" });

        _mediatr = mockMediator.Object;

        // Test queryleri
        _mevoraQuery = new MevoraGetAllDataQuery();
        _mediatrQuery = new MediatrGetAllDataQuery();
    }

    [Benchmark]
    public async Task<string[]> MevoraDispatcherTest()
    {
        return await _mevoraDispatcher.DispatchAsync(_mevoraQuery);
    }

    [Benchmark]
    public async Task<string[]> MediatrTest()
    {
        return await _mediatr.Send(_mediatrQuery);
    }
}