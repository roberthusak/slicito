using Slicito.Abstractions;
using Slicito.Abstractions.Queries;
using Slicito.ProgramAnalysis;

namespace Slicito.DotNet;

public class DotNetTypes(ITypeSystem typeSystem) : IProgramTypes
{
    public LinkType Contains { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, "Contains")]);
    public LinkType Calls { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, "Calls")]);

    public ElementType Project { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Project")]);
    public ElementType Namespace { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Namespace")]);
    public ElementType Type { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Type")]);
    public ElementType Property { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Property")]);
    public ElementType Field { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Field")]);
    public ElementType Method { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Method")]);
    public ElementType Operation { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Operation")]);

    public ElementType Assignment { get; } = GetOperationType(typeSystem, "Assignment");
    public ElementType ConditionalJump { get; } = GetOperationType(typeSystem, "ConditionalJump");
    public ElementType Call { get; } = GetOperationType(typeSystem, "Call");

    ElementType IProgramTypes.Procedure => Method;

    bool IProgramTypes.HasName(ElementType elementType) => true;

    private static ElementType GetOperationType(ITypeSystem typeSystem, string operationKind) =>
        typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Operation"), (DotNetAttributeNames.OperationKind, operationKind)]);
}
