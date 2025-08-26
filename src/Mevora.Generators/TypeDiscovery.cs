using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Mevora.Generators;

internal static class TypeDiscovery
{
    public static INamedTypeSymbol? GetTypes(GeneratorSyntaxContext context)
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
}
