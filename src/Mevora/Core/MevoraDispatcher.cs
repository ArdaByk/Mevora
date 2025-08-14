namespace Mevora;

public class MevoraDispatcher : IMevoraDispatcher
{
    public TResponse Dispatch<TResponse>(IRequest<TResponse> request)
    {
        throw new NotImplementedException();
    }

    public void Dispatch(IRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DispatchAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
