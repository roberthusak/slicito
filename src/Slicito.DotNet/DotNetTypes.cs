using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.ProgramAnalysis;

namespace Slicito.DotNet;

public class DotNetTypes(ITypeSystem typeSystem) : IProgramTypes
{
    public LinkType Contains { get; } = typeSystem.GetLinkType([(CommonAttributeNames.Kind, CommonAttributeValues.Kind.Contains)]);
    public LinkType References { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.References)]);
    public LinkType Overrides { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Overrides)]);
    public LinkType Calls { get; } = typeSystem.GetLinkType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Calls)]);

    public ElementType Solution { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Solution)]);
    public ElementType Project { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Project)]);
    public ElementType Namespace { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Namespace)]);
    public ElementType Type { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Type)]);
    public ElementType Property { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Property)]);
    public ElementType Field { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Field)]);
    public ElementType Method { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Method)]);
    public ElementType LocalFunction { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.LocalFunction)]);
    public ElementType Lambda { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Lambda)]);
    public ElementType Operation { get; } = typeSystem.GetElementType([(DotNetAttributeNames.Kind, DotNetAttributeValues.Kind.Operation)]);

    internal ElementType SymbolTypes { get; } =
        typeSystem.GetElementType([(
            DotNetAttributeNames.Kind,
            [
                DotNetAttributeValues.Kind.Namespace,
                DotNetAttributeValues.Kind.Type,
                DotNetAttributeValues.Kind.Property,
                DotNetAttributeValues.Kind.Field,
                DotNetAttributeValues.Kind.Method
            ]
        )]);

    public ElementType Assignment { get; } = GetOperationType(typeSystem, DotNetAttributeValues.OperationKind.Assignment);
    public ElementType ConditionalJump { get; } = GetOperationType(typeSystem, DotNetAttributeValues.OperationKind.ConditionalJump);
    public ElementType Call { get; } = GetOperationType(typeSystem, DotNetAttributeValues.OperationKind.Call);

    ElementType IProgramTypes.Procedure => Method;
    public ElementType NestedProcedures => LocalFunction | Lambda;

    bool IProgramTypes.HasName(ElementType elementType) => true;

    public bool HasCodeLocation(ElementType elementType) => elementType.Value.IsSubsetOfOrEquals(SymbolTypes.Value);

    private static ElementType GetOperationType(ITypeSystem typeSystem, string operationKind) =>
        typeSystem.GetElementType([(DotNetAttributeNames.Kind, "Operation"), (DotNetAttributeNames.OperationKind, operationKind)]);
}
