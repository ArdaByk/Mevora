using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace Mevora.Generators;

internal class MessagePublisherGenerator
{
    private readonly Compilation _compilation;
    private readonly ImmutableArray<INamedTypeSymbol> _allTypes;

    public MessagePublisherGenerator(Compilation compilation, ImmutableArray<INamedTypeSymbol> allTypes)
    {
        _compilation = compilation;
        _allTypes = allTypes;
    }

    public void Generate(SourceBuilder sb)
    {
        var iMessage = _compilation.GetTypeByMetadataName("Mevora.IMessage");
        if (iMessage == null) return;

        var messageTypes = _allTypes
            .Where(t => t.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iMessage)))
            .ToArray();

        foreach (var msgType in messageTypes)
        {
            var msgTypeName = msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            sb.Append($@"    
    public async Task PublishAsync({msgTypeName} message, CancellationToken cancellationToken = default)
    {{
        var delegates = GetCachedMessageDelegates<{msgTypeName}>();
        
        foreach (var dlg in delegates)
        {{
            await dlg(message, cancellationToken);
        }}
    }}
");
        }
    }
}
