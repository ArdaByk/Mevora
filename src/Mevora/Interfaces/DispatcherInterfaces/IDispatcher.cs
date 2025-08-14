namespace Mevora;

public interface IDispatcher
{
    Task<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task DispatchAsync(IRequest request, CancellationToken cancellationToken = default);
    TResponse Dispatch<TResponse>(IRequest<TResponse> request);
    void Dispatch(IRequest request);
}
