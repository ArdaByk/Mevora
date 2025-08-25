using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mevora.Generators;

[Generator]
public class MevoraDispatcherInterfaceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ---- CLASSLARI YAKALA ----
        var classDeclarations = context.SyntaxProvider
           .CreateSyntaxProvider(
               predicate: static (s, _) => s is ClassDeclarationSyntax,
               transform: static (ctx, _) =>
               {
                   return ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
               })
           .Where(static m => m is not null);

        // ---- REQUEST PROCESSORLAR ----
        var processorTypes = classDeclarations
            .Select((symbol, _) =>
            {
                // <CHANGE> Updated to only look for async processors since sync was removed
                var processorInterface = symbol!.AllInterfaces.FirstOrDefault(i =>
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessorAsync<TRequest>" && i.TypeArguments.Length == 1) ||
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessorAsync<TRequest, TResponse>" && i.TypeArguments.Length == 2) ||
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IMessageProcessor<TMessage>" && i.TypeArguments.Length == 1)
                   );

                if (processorInterface == null)
                    return null;

                INamedTypeSymbol? responseType = null;
                if (processorInterface.TypeArguments.Length > 1)
                    responseType = processorInterface.TypeArguments[1] as INamedTypeSymbol;

                bool isAsync = processorInterface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("Async");

                return new
                {
                    ProcessorClass = symbol,
                    IsAsync = isAsync,
                    IsRequestProcessor = processorInterface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("Request"),
                    RequestType = processorInterface.TypeArguments[0],
                    ResponseType = responseType
                };
            })
            .Where(static m => m is not null);

        // ---- IMessage IMPLEMENT EDEN SINIFLAR ----
        var messageTypes = classDeclarations
            .Where(symbol => symbol!.AllInterfaces.Any(i =>
                i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IMessage"))
            .Select((symbol, _) => symbol)
            .Where(static m => m is not null);

        // ---- OUTPUT ----
        context.RegisterSourceOutput(processorTypes.Collect().Combine(messageTypes.Collect()), (spc, collected) =>
        {
            var (processors, messages) = collected;

            var items = processors.Where(x => x != null).ToArray();
            var messageItems = messages.Distinct(SymbolEqualityComparer.Default).ToArray();

            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace Mevora;");
            sb.AppendLine();
            sb.AppendLine("public interface IMevoraDispatcher");
            sb.AppendLine("{");

            // ---- Dispatch metodları - only async since sync was removed ----
            foreach (var item in items)
            {
                if (item!.IsRequestProcessor)
                {
                    var reqType = item.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    // <CHANGE> Only generate async methods since sync was removed
                    if (item.ResponseType != null)
                    {
                        var respType = item.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        sb.AppendLine($"    Task<{respType}> DispatchAsync({reqType} request, CancellationToken cancellationToken = default);");
                    }
                    else
                    {
                        sb.AppendLine($"    Task DispatchAsync({reqType} request, CancellationToken cancellationToken = default);");
                    }
                }
            }

            // ---- Publish metodları ----
            foreach (var msg in messageItems)
            {
                var msgType = msg!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"    Task PublishAsync({msgType} message, CancellationToken cancellationToken = default);");
            }

            sb.AppendLine("}");

            spc.AddSource("IMevoraDispatcher.g.cs", sb.ToString());
        });
    }
}