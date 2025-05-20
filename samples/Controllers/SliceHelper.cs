using Slicito.Abstractions;
using Slicito.Common;
using Slicito.Abstractions.Facts;

namespace Controllers;

public static class SliceHelper
{
    public static ISlice CreateSampleSlice(ITypeSystem typeSystem)
    {
        var containsType = typeSystem.GetLinkType([("Kind", "Contains")]);
        var isFollowedByType = typeSystem.GetLinkType([("Kind", "IsFollowedBy")]);
        var callsType = typeSystem.GetLinkType([("Kind", "Calls")]);
        var namespaceType = typeSystem.GetElementType([("Kind", "Namespace")]);
        var functionType = typeSystem.GetElementType([("Kind", "Function")]);
        var namespaceOrFunctionType = namespaceType | functionType;
        var operationType = typeSystem.GetElementType([("Kind", "Operation")]);
        var assignmentOperationType = typeSystem.GetElementType([("Kind", "Operation"), ("OperationKind", "Assignment")]);
        var invocationOperationType = typeSystem.GetElementType([("Kind", "Operation"), ("OperationKind", "Invocation")]);

        return new SliceBuilder()
            .AddElementAttribute(namespaceType, "Name", id => new("namespace " + id.Value))
            .AddElementAttribute(functionType, "Name", id => new("function " + id.Value))
            .AddElementAttribute(operationType, "Name", id => new(id.Value))
            .AddRootElements(namespaceType, () => new([new(new("root")), new(new("dependency"))]))
            .AddHierarchyLinks(containsType, namespaceType, namespaceOrFunctionType, sourceId => new(sourceId.Value switch
            {
                "root" =>
                [
                    new(new(new("root::main"), functionType)),
                    new(new(new("root::helper"), functionType)),
                    new(new(new("root::internal"), namespaceType)),
                ],
                "root::internal" =>
                [
                    new(new(new("root::internal::compute"), functionType)),
                ],
                "dependency" =>
                [
                    new(new(new("dependency::external_function"), functionType)),
                ],
                _ => []
            }))
            .AddHierarchyLinks(containsType, functionType, operationType, sourceId => new(sourceId.Value switch
            {
                "root::main" =>
                [
                    new(new(new("root::main::assignment1"), assignmentOperationType)),
                    new(new(new("root::main::call"), invocationOperationType)),
                    new(new(new("root::main::assignment2"), assignmentOperationType)),
                ],
                "root::helper" =>
                [
                    new(new(new("root::helper::call1"), invocationOperationType)),
                    new(new(new("root::helper::call2"), invocationOperationType)),
                    new(new(new("root::helper::call3"), invocationOperationType)),
                ],
                "root::internal::compute" =>
                [
                    new(new(new("root::internal::compute::assignment1"), assignmentOperationType)),
                    new(new(new("root::internal::compute::assignment2"), assignmentOperationType)),
                ],
                "dependency::external_function" =>
                [
                    new(new(new("dependency::external_function::assignment"), assignmentOperationType)),
                ],
                _ => []
            }))
            .AddLinks(isFollowedByType, operationType, operationType, sourceId => new(sourceId.Value switch
            {
                "root::main::assignment1" => new(new(new("root::main::call"), invocationOperationType)),
                "root::main::call" => new(new(new("root::main::assignment2"), assignmentOperationType)),
                "root::helper::call1" => new(new(new("root::helper::call2"), invocationOperationType)),
                "root::helper::call2" => new(new(new("root::helper::call3"), invocationOperationType)),
                "root::internal::compute::assignment1" => new(new(new("root::internal::compute::assignment2"), assignmentOperationType)),
                _ => (ISliceBuilder.PartialLinkInfo?) null
            }))
            .AddLinks(callsType, invocationOperationType, functionType, sourceId => new(sourceId.Value switch
            {
                "root::main::call" => new(new(new("root::helper"))),
                "root::helper::call1" => new(new(new("root::internal::compute"))),
                "root::helper::call2" => new(new(new("dependency::external_function"))),
                "root::helper::call3" => new(new(new("dependency::external_function"))),
                _ => (ISliceBuilder.PartialLinkInfo?) null
            }))
            .Build();
    }
}
