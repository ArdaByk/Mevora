using Microsoft.CodeAnalysis;

namespace Mevora.Generators;

internal sealed class MessageDescriptor
{
    public INamedTypeSymbol MessageType { get; }

    public MessageDescriptor(INamedTypeSymbol messageType)
    {
        MessageType = messageType;
    }
}
