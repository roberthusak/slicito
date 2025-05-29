using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal static class ElementIdProvider
{
    private static readonly SymbolDisplayFormat _projectUniqueNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: 
            SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeContainingType |
            SymbolDisplayMemberOptions.IncludeExplicitInterface,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType,
        extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static ElementId GetId(Solution solution) => new(solution.FilePath!);

    public static ElementId GetId(Project project) => new(project.FilePath!);

    public static ElementId GetId(Project project, ISymbol symbol) =>
        new($"{GetProjectUniqueName(project)}.{GetUniqueNameWithinProject(symbol)}");

    public static string GetOperationIdPrefix(Project project, IMethodSymbol method) => $"{GetId(project, method).Value}.op!";

    public static ElementId GetMethodIdFromOperationId(ElementId operationId)
    {
        var index = operationId.Value.LastIndexOf(".op!", StringComparison.Ordinal);
        if (index == -1)
        {
            throw new ArgumentException("The operation ID is not valid.", nameof(operationId));
        }
        return new(operationId.Value[..index]);
    }

    private static string GetProjectUniqueName(Project project)
    {
        if (project.Solution.FilePath is null)
        {
            return project.Name;
        }
        else
        {
            var solutionName = Path.GetFileNameWithoutExtension(project.Solution.FilePath);

            return $"{solutionName}.{project.Name}";
        }
    }

    private static string GetUniqueNameWithinProject(ISymbol symbol)
    {
        var name = symbol.ToDisplayString(_projectUniqueNameFormat);

        if (symbol is IMethodSymbol method)
        {
            // Used to distinguish these two cases
            if (method.MethodKind == MethodKind.Constructor)
            {
                return name + ".ctor";
            }
            if (method.MethodKind == MethodKind.StaticConstructor)
            {
                return name + ".cctor";
            }
        }

        return name;
    }
}
