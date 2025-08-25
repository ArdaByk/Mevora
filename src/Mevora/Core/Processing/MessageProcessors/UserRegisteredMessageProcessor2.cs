using Mevora.Models.Messages;

namespace Mevora.Core.Processing.MessageProcessors;

public class UserRegisteredMessageProcessor2 : IMessageProcessor<UserRegisteredMessage>
{
    public Task Run(UserRegisteredMessage message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("sent ss.");
    }
}
