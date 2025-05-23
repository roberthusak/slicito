using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal static class ElementIdProvider
{
    private static readonly SymbolDisplayFormat _projectUniqueNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static ElementId GetId(Solution solution) => new(solution.FilePath!);

    public static ElementId GetId(Project project) => new(project.FilePath!);

    public static ElementId GetId(INamespaceSymbol @namespace) => GetSymbolId(@namespace);

    public static ElementId GetId(ITypeSymbol type) => GetSymbolId(type);

    public static ElementId GetId(IPropertySymbol property) => GetSymbolId(property);

    public static ElementId GetId(IFieldSymbol field) => GetSymbolId(field);

    public static ElementId GetId(IMethodSymbol method) => GetSymbolId(method);
    
    public static string GetOperationIdPrefix(IMethodSymbol method) => $"{GetId(method).Value}.op!";

    public static ElementId GetMethodIdFromOperationId(ElementId operationId)
    {
        var index = operationId.Value.LastIndexOf(".op!", StringComparison.Ordinal);
        if (index == -1)
        {
            throw new ArgumentException("The operation ID is not valid.", nameof(operationId));
        }
        return new(operationId.Value.Substring(0, index));
    }

    private static ElementId GetSymbolId(ISymbol symbol) =>
        new($"{GetAssemblyName(symbol)}.{GetUniqueNameWithinProject(symbol)}");

    private static string GetAssemblyName(ISymbol symbol)
    {
        var name = symbol.ContainingAssembly?.Name;
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("The symbol does not belong to a named assembly.");
        }

        return name!;
    }

    private static string GetUniqueNameWithinProject(ISymbol symbol) => symbol.ToDisplayString(_projectUniqueNameFormat);
}
