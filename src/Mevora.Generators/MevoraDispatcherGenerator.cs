using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
                transform: static (ctx, _) => GetRequestType(ctx))
            .Where(static m => m is not null)
            .Collect();

        var compilationAndRequests = context.CompilationProvider.Combine(requestDeclarations);

        context.RegisterSourceOutput(compilationAndRequests,
            static (spc, source) => GenerateDispatcher(spc, source.Left, source.Right!));
    }

    private static INamedTypeSymbol? GetRequestType(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        var implementsTargetInterfaces = typeSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString() == "IRequest" ||
            (i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequest<TResponse>") ||

            (i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest>") ||

            (i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest, TResponse>") ||

            (i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest>") ||

            (i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest, TResponse>")
);

        return implementsTargetInterfaces ? typeSymbol : null;
    }


    private static void GenerateDispatcher(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var sb = new StringBuilder();

        if (requestTypes.IsDefaultOrEmpty)
            sb = new StringBuilder(@"using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mevora
{
    public class MevoraDispatcher: IMevoraDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, CancellationToken, Task>> _asyncVoidDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, CancellationToken, Task<object>>> _asyncGenericDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Action<IServiceProvider, IRequest>> _syncVoidDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, object>> _syncGenericDispatchers = new();

        public MevoraDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
     }

}");
        sb = new StringBuilder(@"using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mevora
{
    public class MevoraDispatcher: IMevoraDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, CancellationToken, Task>> _asyncVoidDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, CancellationToken, Task<object>>> _asyncGenericDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Action<IServiceProvider, IRequest>> _syncVoidDispatchers = new();
        //private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IRequest, object>> _syncGenericDispatchers = new();

        public MevoraDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }");

        GenerateDispatchMethods(sb, compilation, requestTypes);

        sb.Append(@"
    }
}");

        context.AddSource("MevoraDispatcher.g.cs", sb.ToString());
    }

    private static void GenerateDispatchMethods(StringBuilder sb, Compilation compilation, ImmutableArray<INamedTypeSymbol> requestTypes)
    {
        var requests = requestTypes.Where(r => r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequest<TResponse>") || r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequest"));
        var processors = requestTypes
            .Where(r =>
            r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest, TResponse>") ||
            r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest>") ||
            r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest, TResponse>") ||
            r.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest>"));

        foreach (var request in requests)
        {
            foreach (var processor in processors)
            {

                foreach (var iface in processor.AllInterfaces)
                {
                    if (!iface.IsGenericType) continue;
                    var def = iface.ConstructedFrom;
                    var args = iface.TypeArguments;

                    if ((def.ToDisplayString() == "Mevora.IRequestProcessor<TRequest, TResponse>" ||
                         def.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest, TResponse>")
                        && args.Length == 2)
                    {
                        var reqArg = args[0];
                        if (SymbolEqualityComparer.Default.Equals(reqArg, request))
                            MakeGenericDispatchMethod(sb, request, processor);
                        continue;
                    }

                    if ((def.ToDisplayString() == "Mevora.IRequestProcessor<TRequest>" ||
                         def.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest>")
                        && args.Length == 1)
                    {
                        var reqArg = args[0];
                        if (SymbolEqualityComparer.Default.Equals(reqArg, request))
                            MakeDispatchMethod(sb, request, processor);
                        continue;
                    }
                }


            }
        }

    }

    private static void MakeDispatchMethod(StringBuilder sb, INamedTypeSymbol request, INamedTypeSymbol processor)
    {
        if (processor.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest>"))
        {
            sb.Append($@"
        public async Task DispatchAsync({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request, CancellationToken cancellationToken = default)
        {{
            var processor = _serviceProvider.GetRequiredService<global::Mevora.IRequestProcessorAsync<{request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>>();
            await processor.ProcessAsync(request, cancellationToken);
        }}
");
        }

        if (processor.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest>"))
        {
            sb.Append($@"
        public void Dispatch({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request)
        {{
            var processor = _serviceProvider.GetRequiredService<global::Mevora.IRequestProcessor<{request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>>();
            processor.Process(request);
        }}
");
        }
    }

    private static void MakeGenericDispatchMethod(StringBuilder sb, INamedTypeSymbol request, INamedTypeSymbol processor)
    {
        var responseArgType = request.AllInterfaces.First(i => i.IsGenericType && i.ConstructedFrom.ToDisplayString() == "Mevora.IRequest<TResponse>");
        var responseArg = responseArgType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (processor.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessorAsync<TRequest, TResponse>"))
        {
            sb.Append($@"
        public async Task<{responseArg}> DispatchAsync({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request, CancellationToken cancellationToken = default)
        {{
            var processor = _serviceProvider.GetRequiredService<global::Mevora.IRequestProcessorAsync<{request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {responseArg}>>();
            return await processor.ProcessAsync(request, cancellationToken);
        }}
");
        }

        if (processor.AllInterfaces.Any(i => i.ConstructedFrom.ToDisplayString() == "Mevora.IRequestProcessor<TRequest, TResponse>"))
        {
            sb.Append($@"
        public {responseArg} Dispatch({request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} request)
        {{
            var processor = _serviceProvider.GetRequiredService<global::Mevora.IRequestProcessor<{request.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {responseArg}>>();
            return processor.Process(request);
        }}
");
        }
    }

}
