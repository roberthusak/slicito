using Microsoft.CodeAnalysis;

namespace Slicito.DotNet.Implementation;

internal static class RoslynHelper
{
    private static readonly SymbolDisplayFormat _fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
    );

    public static string GetFullName(ISymbol symbol) => symbol.ToDisplayString(_fullNameFormat);

    public static IMethodSymbol GetContainingMethodOrSelf(IMethodSymbol method)
    {
        while (method.MethodKind == MethodKind.LocalFunction || method.MethodKind == MethodKind.AnonymousFunction)
        {
            method = method.ContainingSymbol as IMethodSymbol
                ?? throw new InvalidOperationException($"Method '{method.Name}' is a nested function but is not contained in a method.");
        }

        return method;
    }

    public static bool TryGetPropertyBackingField(IPropertySymbol property, out IFieldSymbol? backingField)
    {
        foreach (var field in property.ContainingType.GetMembers().OfType<IFieldSymbol>())
        {
            if (SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property))
            {
                backingField = field;
                return true;
            }
        }

        backingField = null;
        return false;
    }

    public static bool IsPropertyAutoImplemented(IPropertySymbol property) => TryGetPropertyBackingField(property, out _);
}
