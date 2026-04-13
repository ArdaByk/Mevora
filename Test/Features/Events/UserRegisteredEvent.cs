using Mevora;

namespace Test.Features.Events;

public class UserRegisteredEvent : IMessage
{
    public string Username { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

public class SendWelcomeEmailProcessor : IMessageProcessor<UserRegisteredEvent>
{
    private readonly ILogger<SendWelcomeEmailProcessor> _logger;

    public SendWelcomeEmailProcessor(ILogger<SendWelcomeEmailProcessor> logger)
    {
        _logger = logger;
    }

    public Task Run(UserRegisteredEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending welcome email to: {Username}", message.Username);
        return Task.CompletedTask;
    }
}

public class UpdateUserStatisticsProcessor : IMessageProcessor<UserRegisteredEvent>
{
    private readonly ILogger<UpdateUserStatisticsProcessor> _logger;

    public UpdateUserStatisticsProcessor(ILogger<UpdateUserStatisticsProcessor> logger)
    {
        _logger = logger;
    }

    public Task Run(UserRegisteredEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating statistics for: {Username}", message.Username);
        return Task.CompletedTask;
    }
}
