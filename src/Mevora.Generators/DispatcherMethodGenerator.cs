using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Mevora.Generators;

internal class DispatcherMethodGenerator
{
    private readonly Compilation _compilation;
    private readonly ImmutableArray<INamedTypeSymbol> _requestTypes;

    public DispatcherMethodGenerator(Compilation compilation, ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        _compilation = compilation;
        _requestTypes = requestTypes;
    }

    public void Generate(SourceBuilder sb)
    {
        var iRequest = _compilation.GetTypeByMetadataName("Mevora.IRequest");
        var iRequestOfT = _compilation.GetTypeByMetadataName("Mevora.IRequest`1");
        var iProcAsyncOfT = _compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`1");
        var iProcAsyncOfTT = _compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`2");
        var iRequestValidator = _compilation.GetTypeByMetadataName("Mevora.IRequestValidator`1");

        bool Implements(INamedTypeSymbol t, INamedTypeSymbol? def)
            => def is not null && t.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, def));

        var requests = _requestTypes.Where(r => Implements(r, iRequest) || Implements(r, iRequestOfT)).ToArray();
        var processors = _requestTypes.Where(r =>
            r.AllInterfaces.Any(i =>
                (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfT)) ||
                (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfTT))
            )).ToArray();

        var validators = _requestTypes.Where(v => v.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestValidator))).ToList();

        foreach (var request in requests)
        {
            foreach (var processor in processors)
            {
                foreach (var iface in processor.AllInterfaces)
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.OriginalDefinition;
                    var args = iface.TypeArguments;

                    bool hasValidator = validators.Any(v =>
                        v.AllInterfaces
                            .Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestValidator))
                            .Any(i => i.TypeArguments.Length > 0 && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], request))
                    );

                    if (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(def, iProcAsyncOfTT))
                    {
                        if (args.Length == 2 && SymbolEqualityComparer.Default.Equals(args[0], request))
                        {
                            MakeGenericDispatchMethod(sb, request, args[1], processor, def, hasValidator);
                            break;
                        }
                    }

                    if (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(def, iProcAsyncOfT))
                    {
                        if (args.Length == 1 && SymbolEqualityComparer.Default.Equals(args[0], request))
                        {
                            MakeDispatchMethod(sb, request, processor, def, hasValidator);
                            break;
                        }
                    }
                }
            }
        }
    }

    private static void MakeDispatchMethod(SourceBuilder sb, INamedTypeSymbol request, INamedTypeSymbol processor, INamedTypeSymbol processorInterface, bool hasValidator)
    {
        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string interfaceWithGeneric = $"IRequestProcessorAsync<{requestTypeName}>";

        sb.Append($@"
    public async Task DispatchAsync({requestTypeName} request, CancellationToken cancellationToken = default)
    {{
        {(hasValidator ? "await ValidateRequestAsync(request);" : "")}
        var pipelineActions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<IPipelineAction<{requestTypeName}>>(_serviceProvider).ToArray();
        var processor = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{interfaceWithGeneric}>(_serviceProvider);
        ProcessorDelegate processorDelegate = () => processor.ProcessAsync(request, cancellationToken);
        
        await ExecutePipelineAsync(pipelineActions, request, cancellationToken, processorDelegate);
    }}");
    }

    private static void MakeGenericDispatchMethod(SourceBuilder sb, INamedTypeSymbol request, ITypeSymbol responseType, INamedTypeSymbol processor, INamedTypeSymbol processorInterface, bool hasValidator)
    {
        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string responseTypeName = responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string interfaceWithGeneric = $"IRequestProcessorAsync<{requestTypeName}, {responseTypeName}>";

        sb.Append($@"
    public async Task<{responseTypeName}> DispatchAsync({requestTypeName} request, CancellationToken cancellationToken = default)
    {{
        {(hasValidator ? "await ValidateRequestAsync(request);" : "")}
        var pipelineActions = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<IPipelineAction<{requestTypeName}, {responseTypeName}>>(_serviceProvider).ToArray();
        var processor = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{interfaceWithGeneric}>(_serviceProvider);
        ProcessorDelegate<{responseTypeName}> processorDelegate = () => processor.ProcessAsync(request, cancellationToken);
        
        return await ExecutePipelineAsync(pipelineActions, request, cancellationToken, processorDelegate);
    }}");
    }
}
