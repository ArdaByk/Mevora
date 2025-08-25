namespace Mevora;

public delegate Task<TResponse> ProcessorDelegate<TResponse>();

public delegate Task ProcessorDelegate();

public interface IPipelineAction<in TRequest, TResponse>
    where TRequest : IRequest
{
    Task<TResponse> Run(TRequest request, ProcessorDelegate<TResponse> next, CancellationToken cancellationToken);
}

public interface IPipelineAction<in TRequest>
    where TRequest : IRequest
{
    Task Run(TRequest request, ProcessorDelegate next, CancellationToken cancellationToken);
}
