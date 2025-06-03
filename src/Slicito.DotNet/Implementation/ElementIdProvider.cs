using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

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

    public static ElementId GetId(Project? project, string? assemblyReferencePath, ISymbol symbol) =>
        new($"{GetUniqueContextPrefix(project, assemblyReferencePath)}.{GetUniqueNameWithinProject(symbol)}");

    public static string GetOperationIdPrefix(Project project, IMethodSymbol method) => $"{GetId(project, null, method).Value}.op!";

    public static ElementId GetMethodIdFromOperationId(ElementId operationId)
    {
        var index = operationId.Value.LastIndexOf(".op!", StringComparison.Ordinal);
        if (index == -1)
        {
            throw new ArgumentException("The operation ID is not valid.", nameof(operationId));
        }
        return new(operationId.Value[..index]);
    }

    private static string GetUniqueContextPrefix(Project? project, string? assemblyReferencePath)
    {
        if (project is not null)
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
        else if (assemblyReferencePath is not null)
        {
            return assemblyReferencePath;
        }
        else
        {
            throw new InvalidOperationException("Either a project or an assembly reference path must be provided.");
        }
    }

    private static string GetUniqueNameWithinProject(ISymbol symbol)
    {
        var name = symbol.ToDisplayString(_projectUniqueNameFormat);

        if (symbol is IMethodSymbol method)
        {
            // These cases are not distinguishable by name
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                    name += ".ctor";
                    break;
                case MethodKind.StaticConstructor:
                    name += ".cctor";
                    break;
                case MethodKind.AnonymousFunction:
                    var containingMethod = RoslynHelper.GetContainingMethodOrSelf(method);
                    var location = method.Locations.First().SourceSpan;
                    name = $"{GetUniqueNameWithinProject(containingMethod)}.lambda_{location.Start}-{location.End}";
                    break;
            }
        }

        if (TryGetTypeArgumentsThatAreTypeParametersFromOutside(symbol, out var typeParameters))
        {
            // Ensures that A<B.T> and A<C.T> are distinguishable
            var typeParametersString = string.Join(
                ",",
                typeParameters.Select(t =>
                    $"{t.ContainingSymbol.ToDisplayString(_projectUniqueNameFormat)}.{t.Name}"));

            name += $"[{typeParametersString}]";
        }

        return name;
    }

    private static bool TryGetTypeArgumentsThatAreTypeParametersFromOutside(
        ISymbol symbol,
        [NotNullWhen(true)] out IReadOnlyList<ITypeParameterSymbol>? typeParameters)
    {
        List<ITypeParameterSymbol>? typeParametersList = null;
        
        for (var current = symbol; current is not null or INamespaceSymbol; current = current.ContainingSymbol)
        {
            ImmutableArray<ITypeParameterSymbol> currentTypeParameters;
            ImmutableArray<ITypeSymbol> currentTypeArguments;
            if (current is IMethodSymbol method)
            {
                currentTypeParameters = method.TypeParameters;
                currentTypeArguments = method.TypeArguments;
            }
            else if (current is INamedTypeSymbol type)
            {
                currentTypeParameters = type.TypeParameters;
                currentTypeArguments = type.TypeArguments;
            }
            else
            {
                continue;
            }

            for (var i = 0; i < currentTypeParameters.Length; i++)
            {
                var typeParameter = currentTypeParameters[i];
                var typeArgument = currentTypeArguments[i];

                // We are interested in type arguments that are type parameters from outside
                // (typeArgument is equal to the matching typeParameter when unspecified)
                if (!SymbolEqualityComparer.Default.Equals(typeParameter, typeArgument))
                {
                    if (typeArgument is ITypeParameterSymbol outsideTypeParameter)
                    {
                        typeParametersList ??= [];
                        typeParametersList.Add(outsideTypeParameter);
                    }
                    else if (typeArgument is INamedTypeSymbol namedType && namedType.IsGenericType)
                    {
                        if (TryGetTypeArgumentsThatAreTypeParametersFromOutside(namedType, out var nestedTypeParameters))
                        {
                            typeParametersList ??= [];
                            typeParametersList.AddRange(nestedTypeParameters);
                        }
                    }
                }
            }

            symbol = symbol.ContainingSymbol;
        }

        typeParameters = typeParametersList;
        return typeParametersList is not null;
    }
}
