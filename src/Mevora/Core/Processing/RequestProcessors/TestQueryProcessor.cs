using Mevora.Models.Requests;

namespace Mevora.Core.Processing.RequestProcessors;

public class TestQueryProcessor : IRequestProcessorAsync<TestQuery>
{
    public Task ProcessAsync(TestQuery request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing TestQuery with Name: {request.Name}");
        return Task.CompletedTask;
    }
}
