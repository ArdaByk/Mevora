using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mevora;

public class GetUserByIdQuery : IRequest<string> { public string Name { get; set; } }
public class TestQuery : IRequest { public string Name { get; set; } }

// Unified LoggingBehavior that works for both response and void requests
public class LoggingBehavior<TRequest, TResponse> : IPipelineAction<TRequest, TResponse>
    where TRequest : IRequest
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
        var isVoidRequest = typeof(TResponse) == typeof(object);

        if (isVoidRequest)
        {
            _logger.LogInformation("Executing void request {RequestName}", requestName);
        }
        else
        {
            _logger.LogInformation("Executing request {RequestName} -> {ResponseName}", requestName, responseName);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            if (isVoidRequest)
            {
                _logger.LogInformation("Void request {RequestName} completed successfully in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("Request {RequestName} completed successfully in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

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

public class GetUserByIdQueryProcessor : IRequestProcessorAsync<GetUserByIdQuery, string>
{
    public Task<string> ProcessAsync(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Name);
    }
}
public class TestQueryProcessor : IRequestProcessorAsync<TestQuery>
{
    public Task ProcessAsync(TestQuery request, CancellationToken cancellationToken)
    {
        // Process the request but don't return anything since it's void
        Console.WriteLine($"Processing TestQuery with Name: {request.Name}");
        return Task.CompletedTask;
    }
}

public class UserRegisteredMessage : IMessage { }

public class UserRegisteredMessageProcessor2 : IMessageProcessor<UserRegisteredMessage>
{
    public Task Run(UserRegisteredMessage message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("sent ss.");
    }

}

public class UserRegisteredMessageProcessor : IMessageProcessor<UserRegisteredMessage>
{
    public Task Run(UserRegisteredMessage message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("sent email.");
    }
}

public class TestQueryValidator : IRequestValidator<TestQuery>
{
    public ValidationResult Validate(ValidationContext<TestQuery> context)
    {
        return context.CheckNotEmpty(x => x.Name, "Ad alanı boş olmamalı.")
            .ToResult();
    }
}


public class UserRegisterValidator : IRequestValidator<TestQuery>
{
    public ValidationResult Validate(ValidationContext<TestQuery> context)
    {
        return context.CheckNotEmpty(x => x.Name, "Ad alanı boş olmamalı.")
            .ToResult();
    }
}
