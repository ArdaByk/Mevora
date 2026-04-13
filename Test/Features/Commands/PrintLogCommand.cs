using Mevora;

namespace Test.Features.Commands;

public class PrintLogCommand : IRequest
{
    public string LogText { get; set; } = string.Empty;
}

public class PrintLogCommandHandler : IRequestProcessorAsync<PrintLogCommand>
{
    private readonly ILogger<PrintLogCommandHandler> _logger;

    public PrintLogCommandHandler(ILogger<PrintLogCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(PrintLogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PrintLogCommand Executed: {LogText}", request.LogText);
        return Task.CompletedTask;
    }
}
