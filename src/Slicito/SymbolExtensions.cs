using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Slicito.Roslyn;
using System.Web;

namespace Slicito;

public static class SymbolExtensions
{
    public static string GetNodeId(this ISymbol symbol)
        => symbol.ToDisplayString();

    public static string GetNodeLabelText(this ISymbol symbol)
    {
        var label = symbol.Name;
        if (string.IsNullOrEmpty(label))
        {
            label = symbol.ToDisplayString();
        }

        // E.g. for "<global namespace>"
        return HttpUtility.HtmlEncode(label);
    }

    public static string? GetFileOpenUri(this ISymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault();
        if (location is null || !location.IsInSource)
        {
            return null;
        }

        return ServerUtils.GetOpenFileEndpointUri(location.GetMappedLineSpan());
    }

    public static INamespaceSymbol? FindTopNamespace(this ISymbol symbol)
    {
        var namespaceSymbol = (symbol as INamespaceSymbol) ?? symbol.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return null;
        }

        while (namespaceSymbol.ContainingNamespace is not null && !namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return namespaceSymbol;
    }

    public static IEnumerable<InvocationInfo> FindCallees(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference is null)
        {
            return Enumerable.Empty<InvocationInfo>();
        }

        if (syntaxReference.GetSyntax() is not BaseMethodDeclarationSyntax declaration)
        {
            return Enumerable.Empty<InvocationInfo>();
        }

        var semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);

        return InvocationWalker.FindInvocations(methodSymbol, declaration, semanticModel);
    }
}
