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
               {
                   return ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
               })
           .Where(static m => m is not null);

        var processorTypes = classDeclarations
            .Select((symbol, _) =>
            {
                var processorInterface = symbol!.AllInterfaces.FirstOrDefault(i =>
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessor<TRequest>" && i.TypeArguments.Length == 1) ||
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessor<TRequest, TResponse>" && i.TypeArguments.Length == 2) ||
                     (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessorAsync<TRequest>" && i.TypeArguments.Length == 1) ||
                   (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Mevora.IRequestProcessorAsync<TRequest, TResponse>" && i.TypeArguments.Length == 2)

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
                    RequestType = processorInterface.TypeArguments[0],
                    ResponseType = responseType
                };
            })
            .Where(static m => m is not null);

        context.RegisterSourceOutput(processorTypes.Collect(), (spc, collected) =>
        {
            var items = collected
                .Where(x => x != null)
                .ToArray();

            var sb = new StringBuilder();

            if (items.Length == 0)
            {
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Threading;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine();
                sb.AppendLine("namespace Mevora;");
                sb.AppendLine();
                sb.AppendLine("public interface IMevoraDispatcher");
                sb.AppendLine("{");
                sb.AppendLine("}");
            } else
            {
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
                    var reqType = item!.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (item.IsAsync)
                    {
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
                    else
                    {
                        if (item.ResponseType != null)
                        {
                            var respType = item.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            sb.AppendLine($"    {respType} Dispatch({reqType} request);");
                        }
                        else
                        {
                            sb.AppendLine($"    void Dispatch({reqType} request);");
                        }
                    }
                }

                sb.AppendLine("}");
            }
            spc.AddSource("IMevoraDispatcher.g.cs", sb.ToString());
        });
    }
}
