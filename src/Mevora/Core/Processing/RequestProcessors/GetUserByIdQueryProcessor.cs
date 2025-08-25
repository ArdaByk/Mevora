using Mevora.Models.Requests;

namespace Mevora.Core.Processing.RequestProcessors;

public class GetUserByIdQueryProcessor : IRequestProcessorAsync<GetUserByIdQuery, string>
{
    public Task<string> ProcessAsync(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Name);
    }
}
