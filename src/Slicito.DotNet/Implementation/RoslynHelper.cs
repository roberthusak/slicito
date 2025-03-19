using Microsoft.CodeAnalysis;

namespace Slicito.DotNet.Implementation;

internal static class RoslynHelper
{
    private static readonly SymbolDisplayFormat _fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    public static string GetFullName(ISymbol symbol) => symbol.ToDisplayString(_fullNameFormat);
}
