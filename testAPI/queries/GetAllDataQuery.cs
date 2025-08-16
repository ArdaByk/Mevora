using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MediatR;
using Mevora;
using System.Threading;
using System.Threading.Tasks;

namespace testAPI.queries
{
    public class MevoraGetAllDataQuery : Mevora.IRequest<string[]> { }
    public class MediatrGetAllDataQuery : MediatR.IRequest<string[]> { }

    public class MediatrVsMevoraBenchmark
    {
        private readonly IMediator _mediator;
        private readonly IMevoraDispatcher _mevoraDispatcher;
        private readonly MediatrHandler _mediatrHandler;
        private readonly MevoraHandler _mevoraHandler;

        public MediatrVsMevoraBenchmark()
        {
            _mediatrHandler = new MediatrHandler();
            _mevoraHandler = new MevoraHandler();
        }
    }

    public class MediatrHandler : IRequestHandler<MediatrGetAllDataQuery, string[]>
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<string[]> Handle(MediatrGetAllDataQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Summaries);
        }
    }

    public class MevoraHandler : IRequestProcessorAsync<MevoraGetAllDataQuery, string[]>
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<string[]> ProcessAsync(MevoraGetAllDataQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Summaries);
        }
    }

   
}