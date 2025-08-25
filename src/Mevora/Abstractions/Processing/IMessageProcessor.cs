using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mevora;

public interface IMessage { }

public interface IMessageProcessor<TMessage> where TMessage : IMessage 
{
    Task Run(TMessage message, CancellationToken cancellationToken);
}
