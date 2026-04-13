using Mevora;

namespace Test.Features.Ping;

public class PingRequest : IRequest<string>
{
    public string Message { get; set; } = string.Empty;
}

public class PingRequestHandler : IRequestProcessorAsync<PingRequest, string>
{
    public Task<string> ProcessAsync(PingRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Pong: {request.Message}");
    }
}

public class PingRequestValidator : IRequestValidator<PingRequest>
{
    public ValidationResult Validate(ValidationContext<PingRequest> context)
    {
        return context
            .CheckNotEmpty(x => x.Message, "Message cannot be empty.")
            .CheckMinLength(x => x.Message, 3, "Message must be at least 3 characters long.")
            .ToResult();
    }
}
