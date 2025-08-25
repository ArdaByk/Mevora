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
        var iProcAsyncOfT = comp.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`1");
        var iProcAsyncOfTT = comp.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`2");
        var iMessage = comp.GetTypeByMetadataName("Mevora.IMessage");
        var iMessageProc = comp.GetTypeByMetadataName("Mevora.IMessageProcessor`1");
        var iRequestValidator = comp.GetTypeByMetadataName("Mevora.IRequestValidator`1");

        bool implementsTarget = typeSymbol.AllInterfaces.Any(i =>
            (iRequest is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequest)) ||
            (iRequestOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestOfT)) ||
            (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfT)) ||
            (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfTT)) ||
            (iMessage is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iMessage)) ||
            (iMessageProc is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iMessageProc)) ||
            (iRequestValidator is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestValidator))
        );

        return implementsTarget ? typeSymbol : null;
    }

    private static void GenerateDispatcher(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var sb = new StringBuilder();

        sb.Append(@"

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Mevora;

namespace Mevora;

public partial class MevoraDispatcher : IMevoraDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    
    private static readonly ConcurrentDictionary<Type, object[]> _cachedPipelineActions = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>> _asyncVoidDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _asyncGenericDispatchers = new();
    private static readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task>[]> _cachedMessageDelegates = new();


    public MevoraDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private T[] GetCachedPipelineActions<T>(Type requestType)
    {
        if (!_cachedPipelineActions.TryGetValue(requestType, out var cached))
        {
            var pipelineActions = _serviceProvider.GetServices<T>().ToArray();
            cached = pipelineActions.Cast<object>().ToArray();
            _cachedPipelineActions.TryAdd(requestType, cached);
        }
        return cached.Cast<T>().ToArray();
    }

    private async Task ExecutePipelineAsync<TRequest>(
        IPipelineAction<TRequest>[] pipelineActions, 
        TRequest request, 
        CancellationToken cancellationToken,
        ProcessorDelegate processor)
        where TRequest : IRequest
    {
        if (pipelineActions.Length == 0)
        {
            await processor();
            return;
        }

        ProcessorDelegate pipeline = processor;
        
        for (int i = pipelineActions.Length - 1; i >= 0; i--)
        {
            var pipelineAction = pipelineActions[i];
            var currentPipeline = pipeline;
            pipeline = () => pipelineAction.Run(request, currentPipeline, cancellationToken);
        }
        
        await pipeline();
    }

    private async Task<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        IPipelineAction<TRequest, TResponse>[] pipelineActions, 
        TRequest request, 
        CancellationToken cancellationToken,
        ProcessorDelegate<TResponse> processor)
        where TRequest : IRequest<TResponse>, IRequest
    {
        if (pipelineActions.Length == 0)
        {
            return await processor();
        }

        ProcessorDelegate<TResponse> pipeline = processor;
        
        for (int i = pipelineActions.Length - 1; i >= 0; i--)
        {
            var pipelineAction = pipelineActions[i];
            var currentPipeline = pipeline;
            pipeline = () => pipelineAction.Run(request, currentPipeline, cancellationToken);
        }
        
        return await pipeline();
    }

    private bool HasValidator<TRequest>() where TRequest : IRequest
    {
        return _serviceProvider.GetService<IRequestValidator<TRequest>>() != null;
    }

    private async Task ValidateRequestAsync<TRequest>(TRequest request) where TRequest : IRequest
    {
        var validator = _serviceProvider.GetService<IRequestValidator<TRequest>>();
        if (validator == null)
            return;

        try
        {
            var context = new ValidationContext<TRequest>(request);
            var result = validator.Validate(context);
            
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }
        catch (ValidationException)
        {
            throw; // Re-throw validation exceptions as-is
        }
        catch (Exception ex)
        {
            throw new ValidationException($""Validation failed with error: { ex.Message}"");
        }
    }

  private Func<object, CancellationToken, Task>[] GetCachedMessageDelegates<T>() where T : IMessage
{
    var msgType = typeof(T);

    if (!_cachedMessageDelegates.TryGetValue(msgType, out var cached))
    {
        var delegates = _serviceProvider
            .GetServices<IMessageProcessor<T>>()
            .GroupBy(p => p.GetType()) // duplicate processor'ları engelle
            .Select(g => g.First())
            .Select<IMessageProcessor<T>, Func<object, CancellationToken, Task>>(proc =>
                async (msg, ct) => await proc.Run((T)msg, ct))
            .ToArray();

        _cachedMessageDelegates.TryAdd(msgType, delegates);
        cached = delegates;
    }

    return cached;
}


    
");

        GenerateDispatchMethods(sb, compilation, requestTypes);
        GenerateMessagePublishers(sb, compilation, requestTypes);

        sb.AppendLine("}");

        context.AddSource("MevoraDispatcher.g.cs", sb.ToString());
    }

    private static void GenerateDispatchMethods(StringBuilder sb, Compilation compilation, ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var iRequest = compilation.GetTypeByMetadataName("Mevora.IRequest");
        var iRequestOfT = compilation.GetTypeByMetadataName("Mevora.IRequest`1");
        var iProcAsyncOfT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`1");
        var iProcAsyncOfTT = compilation.GetTypeByMetadataName("Mevora.IRequestProcessorAsync`2");
        var iRequestValidator = compilation.GetTypeByMetadataName("Mevora.IRequestValidator`1");

        bool Implements(INamedTypeSymbol t, INamedTypeSymbol? def)
            => def is not null && t.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, def));

        var requests = requestTypes.Where(r => Implements(r, iRequest) || Implements(r, iRequestOfT)).ToArray();
        var processors = requestTypes.Where(r =>
            r.AllInterfaces.Any(i =>
                (iProcAsyncOfT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfT)) ||
                (iProcAsyncOfTT is not null && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iProcAsyncOfTT))
            )).ToArray();

        // Tüm assembly'deki validator'ları bul
        var validators = requestTypes.Where(v => v.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequestValidator))).ToList();

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

    private static void GenerateMessagePublishers(
        StringBuilder sb,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> allTypes)
    {
        var iMessage = compilation.GetTypeByMetadataName("Mevora.IMessage");
        if (iMessage == null) return;

        var messageTypes = allTypes
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

    private static void MakeDispatchMethod(StringBuilder sb, INamedTypeSymbol request, INamedTypeSymbol processor, INamedTypeSymbol processorInterface, bool hasValidator)
    {
        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string interfaceWithGeneric = $"IRequestProcessorAsync<{requestTypeName}>";

        sb.Append($@"
    public async Task DispatchAsync({requestTypeName} request, CancellationToken cancellationToken = default)
    {{
        {(hasValidator ? "await ValidateRequestAsync(request);" : "")}
        
        var requestType = request.GetType();
        
        if (!_asyncVoidDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher = async (req, ct) =>
                {{
                    var pipelineActions = GetCachedPipelineActions<IPipelineAction<{requestTypeName}>>(requestType);
                    var typedRequest = ({requestTypeName})req;
                    
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    ProcessorDelegate processorDelegate = () => processor.ProcessAsync(typedRequest, ct);
                    
                    await ExecutePipelineAsync(pipelineActions, typedRequest, ct, processorDelegate);
                }};
            _asyncVoidDispatchers.TryAdd(requestType, dispatcher);
        }}

        await dispatcher(request, cancellationToken);
    }}");
    }

    private static void MakeGenericDispatchMethod(StringBuilder sb, INamedTypeSymbol request, ITypeSymbol responseType, INamedTypeSymbol processor, INamedTypeSymbol processorInterface, bool hasValidator)
    {
        string requestTypeName = request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string responseTypeName = responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string interfaceWithGeneric = $"IRequestProcessorAsync<{requestTypeName}, {responseTypeName}>";

        sb.Append($@"
    public async Task<{responseTypeName}> DispatchAsync({requestTypeName} request, CancellationToken cancellationToken = default)
    {{
        {(hasValidator ? "await ValidateRequestAsync(request);" : "")}
        
        var requestType = request.GetType();
        
        if (!_asyncGenericDispatchers.TryGetValue(requestType, out var dispatcher))
        {{
            dispatcher = async (req, ct) =>
                {{
                    var pipelineActions = GetCachedPipelineActions<IPipelineAction<{requestTypeName}, {responseTypeName}>>(requestType);
                    var typedRequest = ({requestTypeName})req;
                    
                    var processor = _serviceProvider.GetRequiredService<{interfaceWithGeneric}>();
                    ProcessorDelegate<{responseTypeName}> processorDelegate = () => processor.ProcessAsync(typedRequest, ct);
                    
                    var result = await ExecutePipelineAsync(pipelineActions, typedRequest, ct, processorDelegate);
                    return (object)result;
                }};
            _asyncGenericDispatchers.TryAdd(requestType, dispatcher);
        }}

        var result = await dispatcher(request, cancellationToken);
        return ({responseTypeName})result;
    }}");
    }
}
