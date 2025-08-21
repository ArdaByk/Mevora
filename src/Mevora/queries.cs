using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mevora;

public class GetUserByIdQuery : IRequest<Guid> { }

public class GetUserByIdQueryProcessor : IRequestProcessorAsync<GetUserByIdQuery, Guid>
{
    public Task<Guid> ProcessAsync(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}

public class LoggingBehavior<TRequest, TResponse> : IPipelineAction<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Run(TRequest request, ProcessorDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var responseName = typeof(TResponse).Name;

        _logger.LogInformation("Executing request {RequestName} -> {ResponseName}", requestName, responseName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogInformation("Request {RequestName} completed successfully in {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
