using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Slicito.Roslyn;
using System.Web;

namespace Slicito;

public static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat _projectUniqueFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat _displayFormat =  new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static string GetUniqueNameWithinProject(this ISymbol symbol) => symbol.ToDisplayString(_projectUniqueFormat);

    public static string GetShortName(this ISymbol symbol, ISymbol? containingNodeSymbol = null)
    {
        var label = symbol.ToDisplayString(_displayFormat);

        // If the node of this symbol is placed under a different node than then one of its containing symbol,
        // prepend its name with all the "missed" symbols up to the top namespace
        if (containingNodeSymbol is not null)
        {
            var ancestorSymbol = symbol.ContainingSymbol;
            while (
                ancestorSymbol != null
                && !SymbolEqualityComparer.Default.Equals(ancestorSymbol, containingNodeSymbol)
                && ancestorSymbol is not INamespaceSymbol { IsGlobalNamespace: true })
            {
                label = $"{ancestorSymbol.GetShortName()}.{label}";

                ancestorSymbol = ancestorSymbol.ContainingSymbol;
            }
        }

        return label;
    }

    public static Uri? GetFileOpenUri(this ISymbol symbol)
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
