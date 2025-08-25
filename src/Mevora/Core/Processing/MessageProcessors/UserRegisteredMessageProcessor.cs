using Mevora.Models.Messages;

namespace Mevora.Core.Processing.MessageProcessors;

public class UserRegisteredMessageProcessor : IMessageProcessor<UserRegisteredMessage>
{
    public Task Run(UserRegisteredMessage message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("sent email.");
    }
}
