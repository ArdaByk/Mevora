using Mevora;
using Microsoft.Extensions.DependencyInjection;

namespace Mevora.UnitTests;

// ────────────────────────────────────────────────────────────
//  Request / Response types
// ────────────────────────────────────────────────────────────

/// <summary>Yanıt döndüren basit bir test isteği.</summary>
public class PingRequest : IRequest<PingResponse> { }

public class PingResponse { public string Pong { get; init; } = "pong"; }

/// <summary>Yanıt döndürmeyen basit yan-etki isteği.</summary>
public class SideEffectRequest : IRequest { public string Data { get; init; } = "data"; }

/// <summary>Validator testi için kullanılan istek — Name boş olmamalı.</summary>
public class ValidatableRequest : IRequest<string>
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
}

// ────────────────────────────────────────────────────────────
//  Message / Event types
// ────────────────────────────────────────────────────────────

public class OrderCreatedMessage : IMessage
{
    public int OrderId { get; init; }
}

// ────────────────────────────────────────────────────────────
//  Handlers
// ────────────────────────────────────────────────────────────

public class PingRequestHandler : IRequestProcessorAsync<PingRequest, PingResponse>
{
    public static int CallCount;

    public Task<PingResponse> ProcessAsync(PingRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref CallCount);
        return Task.FromResult(new PingResponse());
    }
}

public class SideEffectHandler : IRequestProcessorAsync<SideEffectRequest>
{
    public static int CallCount;

    public Task ProcessAsync(SideEffectRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

public class ValidatableRequestHandler : IRequestProcessorAsync<ValidatableRequest, string>
{
    public static int CallCount;

    public Task<string> ProcessAsync(ValidatableRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref CallCount);
        return Task.FromResult("ok");
    }
}

// ────────────────────────────────────────────────────────────
//  Message Processors
// ────────────────────────────────────────────────────────────

public class OrderCreatedHandlerA : IMessageProcessor<OrderCreatedMessage>
{
    public static int CallCount;

    public Task Run(OrderCreatedMessage message, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

public class OrderCreatedHandlerB : IMessageProcessor<OrderCreatedMessage>
{
    public static int CallCount;

    public Task Run(OrderCreatedMessage message, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref CallCount);
        return Task.CompletedTask;
    }
}

// ────────────────────────────────────────────────────────────
//  Pipeline Actions
// ────────────────────────────────────────────────────────────

/// <summary>Execution sırasını takip eden pipeline (response'lu).</summary>
public class TrackingPipelineA<TRequest, TResponse> : IPipelineAction<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _tracker;

    public TrackingPipelineA(List<string> tracker) => _tracker = tracker;

    public async Task<TResponse> Run(TRequest request, ProcessorDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _tracker.Add("A_In");
        var response = await next();
        _tracker.Add("A_Out");
        return response;
    }
}

public class TrackingPipelineB<TRequest, TResponse> : IPipelineAction<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<string> _tracker;

    public TrackingPipelineB(List<string> tracker) => _tracker = tracker;

    public async Task<TResponse> Run(TRequest request, ProcessorDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _tracker.Add("B_In");
        var response = await next();
        _tracker.Add("B_Out");
        return response;
    }
}

// ────────────────────────────────────────────────────────────
//  Validators
// ────────────────────────────────────────────────────────────

public class ValidatableRequestValidator : IRequestValidator<ValidatableRequest>
{
    public ValidationResult Validate(ValidationContext<ValidatableRequest> context)
        => context
            .CheckNotEmpty(r => r.Name, "Name cannot be empty")
            .CheckRange(r => r.Age, 0, 150, "Age must be between 0 and 150")
            .ToResult();
}

// ────────────────────────────────────────────────────────────
//  DI helper
// ────────────────────────────────────────────────────────────

public static class ServiceProviderFactory
{
    /// <summary>Mevora kayıtlı, çözümlenmiş bir ServiceProvider döndürür.</summary>
    public static IServiceProvider Build(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddMevora(cfg =>
            cfg.AddProcessorsFromAssembly(typeof(ServiceProviderFactory).Assembly));

        services.AddMevoraDispatcher();

        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
