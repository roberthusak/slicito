using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.DotNet;

public class DotNetTypes(ITypeSystem typeSystem)
{
    public LinkType Contains { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, "Contains")]);

    public ElementType Project { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Project")]);
    public ElementType Namespace { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Namespace")]);
    public ElementType Type { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Type")]);
    public ElementType Property { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Property")]);
    public ElementType Field { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Field")]);
    public ElementType Method { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Method")]);
}