namespace Mevora;

public interface IRequestProcessorAsync<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken);
}


public interface IRequestProcessorAsync<TRequest>
    where TRequest : IRequest
{
    Task ProcessAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    TResponse Process(TRequest request);
}

public interface IRequestProcessor<TRequest>
    where TRequest : IRequest
{
    void Process(TRequest request);
}
