using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

    public static ElementId GetId(Project project, string? assemblyReferencePath, ISymbol symbol)
    {
        var idBuilder = new StringBuilder();

        AppendUniqueContextPrefix(idBuilder, project, assemblyReferencePath);
        idBuilder.Append(".");
        AppendUniqueNameWithinProject(idBuilder, symbol);

        return new(idBuilder.ToString());
    }

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

    private static void AppendUniqueContextPrefix(StringBuilder prefixBuilder, Project project, string? assemblyReferencePath)
    {
        if (assemblyReferencePath is not null)
        {
            prefixBuilder.Append(assemblyReferencePath);
            prefixBuilder.Append(":");
        }

        if (project.Solution.FilePath is not null)
        {
            prefixBuilder.Append(Path.GetFileNameWithoutExtension(project.Solution.FilePath));
            prefixBuilder.Append(".");
        }

        prefixBuilder.Append(project.Name);
    }

    private static void AppendUniqueNameWithinProject(StringBuilder nameBuilder, ISymbol symbol)
    {
        nameBuilder.Append(symbol.ToDisplayString(_projectUniqueNameFormat));

        if (symbol is IMethodSymbol method)
        {
            // These cases are not distinguishable by name
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                    nameBuilder.Append(".ctor");
                    break;

                case MethodKind.StaticConstructor:
                    nameBuilder.Append(".cctor");
                    break;

                case MethodKind.AnonymousFunction:
                    var lambdaContainingMethod = RoslynHelper.GetContainingMethodOrSelf(method);
                    var location = method.Locations.First().SourceSpan;

                    nameBuilder.Append($"$lambda-{location.Start}-{location.End}-of:");
                    AppendUniqueNameWithinProject(nameBuilder, lambdaContainingMethod);
                    break;

                case MethodKind.LocalFunction:
                    var localContainingMethod = RoslynHelper.GetContainingMethodOrSelf(method);

                    nameBuilder.Append("$local-fn-of:");
                    AppendUniqueNameWithinProject(nameBuilder, localContainingMethod);
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

            nameBuilder.Append($"[{typeParametersString}]");
        }

        if (symbol.ContainingSymbol is ITypeSymbol { IsAnonymousType: true } anonymousType)
        {
            var location = anonymousType.Locations.First().SourceSpan;

            nameBuilder.Append($"$anon-{location.Start}-{location.End}");
        }
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
