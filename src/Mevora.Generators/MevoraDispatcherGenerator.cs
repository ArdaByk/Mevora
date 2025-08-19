using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Mevora.Generators;

[Generator]
internal class MevoraDispatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetTypes(ctx))
            .Where(static m => m is not null)
            .Collect();

        var compilationAndRequests = context.CompilationProvider.Combine(requestDeclarations);

        context.RegisterSourceOutput(compilationAndRequests,
            static (spc, source) => GenerateDispatcher(spc, source.Left, source.Right!));
    }

    private static INamedTypeSymbol? GetTypes(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol)
            return null;

        var comp = context.SemanticModel.Compilation;

        var iRequest = comp.GetTypeByMetadataName("Mevora.IRequest");
        var iRequestOfT = comp.GetTypeByMetadataName("Mevora.IRequest`1");
        var iProcOfT = comp.GetTypeByMetadataName("Mevora.IRequestProcessor`1");
        var iProcOfTT = comp.GetTypeByMetadataName("Mevora.IRequestProcessor`2");
        var iProcAsyncOfT = comp.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`1");
        var iProcAsyncOfTT = comp.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`2");

        bool implementsTarget = typeSymbol.AllInterfaces.Any(i =>
            (iRequest is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequest)) ||
            (iRequestOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestOfT)) ||
            (iProcOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcOfT)) ||
            (iProcOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcOfTT)) ||
            (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfT)) ||
            (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfTT))
        );

        return implementsTarget ? typeSymbol : null;
    }

    private static void GenerateDispatcher(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var sb = new StringBuilder();

        sb.Append(@"using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mevora;

public class MevoraDispatcher: IMevoraDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _asyncVoidDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _asyncGenericDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Action<object>> _syncVoidDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object>> _syncGenericDispatchers = new();

    public MevoraDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }");

        GenerateDispatchMethods(sb, compilation, requestTypes);

        sb.Append(@"
}
");

        context.AddSource("MevoraDispatcher.g.cs", sb.ToString());
    }

    private static void GenerateDispatchMethods(StringBuilder sb, Compilation compilation, ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var iRequest = compilation.GetTypeByMetadataName("Mevora.IRequest");
        var iRequestOfT = compilation.GetTypeByMetadataName("Mevora.IRequest`1");
        var iProc = compilation.GetTypeByMetadataName("Mevora.IRequestProcessor");
        var iProcOfT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessor`1");
        var iProcOfTT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessor`2");
        var iProcAsync = compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync");
        var iProcAsyncOfT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`1");
        var iProcAsyncOfTT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`2");

        bool Implements(INamedTypeSymbol t, INamedTypeSymbol? def)
            => def is not null && t.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, def));

        var requests = requestTypes.Where(r => Implements(r, iRequest) || Implements(r, iRequestOfT)).ToArray();
        var processors = requestTypes.Where(r =>
            r.AllInterfaces.Any(i =>
                (iProc is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProc)) ||
                (iProcOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcOfT)) ||
                (iProcOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcOfTT)) ||
                (iProcAsync is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsync)) ||
                (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfT)) ||
                (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfTT))
            )).ToArray();

        foreach (var request in requests)
        {
            foreach (var processor in processors)
            {
                foreach (var iface in processor.AllInterfaces)
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.OriginalDefinition;
                    var args = iface.TypeArguments;

                    if ((iProcOfTT is not null && SymbolEqualityComparer.Default.Equals(def, iProcOfTT)) ||
                        (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(def, iProcAsyncOfTT)))
                    {
                        if (args.Length == 2 && SymbolEqualityComparer.Default.Equals(args[0], request))
                        {
                            MakeGenericDispatchMethod(sb, request, args[1], processor, def);
                        }
                        continue;
                    }

                    if ((iProcOfT is not null && SymbolEqualityComparer.Default.Equals(def, iProcOfT)) ||
                        (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(def, iProcAsyncOfT)))
                    {
                        if (args.Length == 1 && SymbolEqualityComparer.Default.Equals(args[0], request))
                        {
                            MakeDispatchMethod(sb, request, processor, def);
                        }
                        continue;
                    }
                }
            }
        }
    }

    private static void MakeDispatchMethod(StringBuilder sb, INamedTypeSymbol request, INamedTypeSymbol processor, INamedTypeSymbol processorInterface)
    {
        string processorTypeName = processorInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        bool isAsync = processorTypeName.Contains("IRequestProcessorAsync");

        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        string interfaceName = isAsync ? "global::Mevora.IRequestProcessorAsync" : "global::Mevora.IRequestProcessor";
        string interfaceWithGeneric = $"{interfaceName}<{requestTypeName}>";

        if (isAsync)
        {
            sb.Append($@"
    public async Task DispatchAsync({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request, CancellationToken cancellationToken = default)
    {{
        var requestType = request.GetType();
        
        if (!_asyncVoidDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher = async (req, ct) =>
                {{
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    await processor.ProcessAsync(({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})req, ct);
                }};
            _asyncVoidDispatchers.TryAdd(requestType, dispatcher);
        }}

        await dispatcher(request, cancellationToken);
    }}");
        }
        else
        {
            sb.Append($@"
    public void Dispatch({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request)
    {{
        var requestType = request.GetType();
        
        if (!_syncVoidDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher =  (req) =>
                {{
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    processor.Process(({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})req);
                }};
            _syncVoidDispatchers.TryAdd(requestType, dispatcher);
        }}

        dispatcher(request);
    }}");
        }
    }

    private static void MakeGenericDispatchMethod(StringBuilder sb, INamedTypeSymbol request, ITypeSymbol responseType, INamedTypeSymbol processor, INamedTypeSymbol processorInterface)
    {
        string processorTypeName = processorInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        bool isAsync = processorTypeName.Contains("IRequestProcessorAsync");

        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string responseTypeName = responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        string interfaceName = isAsync ? "global::Mevora.IRequestProcessorAsync" : "global::Mevora.IRequestProcessor";
        string interfaceWithGeneric = $"{interfaceName}<{requestTypeName}, {responseTypeName}>";

        if (isAsync)
        {
            sb.Append($@"
    public async Task<{responseTypeName}> DispatchAsync({requestTypeName} request, CancellationToken cancellationToken = default)
    {{
        var requestType = request.GetType();
        
        if (!_asyncGenericDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher = async (req, ct) =>
                {{
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    var result = await processor.ProcessAsync(({requestTypeName})req, ct);
                    return result;
                }};
            _asyncGenericDispatchers.TryAdd(requestType, dispatcher);
        }}

        var result = await dispatcher(request, cancellationToken);
        return ({responseTypeName})result;
    }}
");
        }
        else
        {
            sb.Append($@"
    public {responseTypeName} Dispatch({requestTypeName} request)
    {{
        var requestType = request.GetType();
        
        if (!_syncGenericDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher = (req) =>
                {{
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    var result = processor.Process(({requestTypeName})req);
                    return result;
                }};
            _syncGenericDispatchers.TryAdd(requestType, dispatcher);
        }}

        var result = dispatcher(request);
        return ({responseTypeName})result;
    }}");
        }
    }
}