using Mevora;
using System.Diagnostics;

namespace Test.Features.Pipelines;

// Generic Pipeline Action that applies to all IRequest<TResponse>
public class PerformanceLoggingAction<TRequest, TResponse> : IPipelineAction<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceLoggingAction<TRequest, TResponse>> _logger;

    public PerformanceLoggingAction(ILogger<PerformanceLoggingAction<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Run(TRequest request, ProcessorDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Handling request with response: {RequestType}", typeof(TRequest).Name);
        
        var response = await next();
        
        sw.Stop();
        _logger.LogInformation("Finished request {RequestType} in {ElapsedMs}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        
        return response;
    }
}

// Generic Pipeline Action that applies to all IRequest
public class SimpleLoggingAction<TRequest> : IPipelineAction<TRequest>
    where TRequest : IRequest
{
    private readonly ILogger<SimpleLoggingAction<TRequest>> _logger;

    public SimpleLoggingAction(ILogger<SimpleLoggingAction<TRequest>> logger)
    {
        _logger = logger;
    }

    public async Task Run(TRequest request, ProcessorDelegate next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling side-effect request: {RequestType}", typeof(TRequest).Name);
        await next();
        _logger.LogInformation("Successfully handled side-effect request: {RequestType}", typeof(TRequest).Name);
    }
}
