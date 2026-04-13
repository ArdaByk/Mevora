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
        var classDeclarations = context.SyntaxProvider
           .CreateSyntaxProvider(
               predicate: static (s, _) => s is ClassDeclarationSyntax,
               transform: static (ctx, _) =>
                   ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol)
           .Where(static m => m is not null);

        var processorTypes = classDeclarations
            .Select((symbol, _) =>
            {
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

                return new ProcessorDescriptor(
                    symbol!,
                    processorInterface.OriginalDefinition
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        .Contains("Request"),
                    processorInterface.TypeArguments[0],
                    responseType
                );
            })
            .Where(static m => m is not null);

        var messageTypes = classDeclarations
            .Where(symbol => symbol!.AllInterfaces.Any(i =>
                i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IMessage"))
            .Select((symbol, _) => new MessageDescriptor(symbol!))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(
            processorTypes.Collect().Combine(messageTypes.Collect()),
            (spc, collected) =>
            {
                var (processors, messages) = collected;

                var items = processors.Where(x => x != null).ToArray();
                var messageItems = messages
                    .GroupBy(m => m!.MessageType, SymbolEqualityComparer.Default)
                    .Select(g => g.First())
                    .ToArray();


                var sb = new StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine("using System.Threading;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine();
                sb.AppendLine("namespace Mevora;");
                sb.AppendLine();
                sb.AppendLine("public interface IMevoraDispatcher");
                sb.AppendLine("{");

                foreach (var item in items)
                {
                    if (item!.IsRequestProcessor)
                    {
                        var reqType = item.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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

                foreach (var msg in messageItems)
                {
                    var msgType = msg!.MessageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    sb.AppendLine($"    Task PublishAsync({msgType} message, CancellationToken cancellationToken = default);");
                }

                sb.AppendLine("}");

                spc.AddSource("IMevoraDispatcher.g.cs", sb.ToString());
            });
    }
}
