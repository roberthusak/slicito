using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal static class ElementIdProvider
{
    public static ElementId GetElementId(Project project) => new(project.FilePath!);

    public static ElementId GetElementId(ElementId containingElementId, INamespaceSymbol @namespace) =>
        new($"{containingElementId.Value}.{@namespace.Name}");

    public static ElementId GetElementId(ElementId containingElementId, ITypeSymbol type) =>
        new($"{containingElementId.Value}.{type.Name}");
}
