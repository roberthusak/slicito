using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Abstractions.Queries;
using Slicito.Queries;

namespace Controllers;

public class SampleStructureBrowser : IController
{
    private const string _openActionName = "Open";
    private const string _idActionParameterName = "Id";

    private readonly ILazySlice _slice;

    private ElementId? _selectedElementId;

    public SampleStructureBrowser(ITypeSystem typeSystem)
    {
        _slice = CreateSampleSlice(typeSystem);
    }

    private static ILazySlice CreateSampleSlice(ITypeSystem typeSystem)
    {
        var containsType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Contains"] } });
        var isFollowedByType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["IsFollowedBy"] } });
        var callsType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Calls"] } });
        var namespaceType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Namespace"] } });
        var functionType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Function"] } });
        var namespaceOrFunctionType = namespaceType.TryGetUnion(functionType)!;
        var operationType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Operation"] } });
        var assignmentOperationType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Operation"] }, { "OperationKind", ["Assignment"] } });
        var invocationOperationType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["Operation"] }, { "OperationKind", ["Invocation"] } });

        return new SliceBuilder()
            .AddElementAttribute(new(namespaceType), "Name", id => new("namespace " + id.Value))
            .AddElementAttribute(new(functionType), "Name", id => new("function " + id.Value))
            .AddElementAttribute(new(operationType), "Name", id => new(id.Value))
            .AddRootElements(new(namespaceType), () => new([new(new("root")), new(new("dependency"))]))
            .AddHierarchyLinks(new(containsType), new(namespaceType), new(namespaceOrFunctionType), sourceId => new(sourceId.Value switch
            {
                "root" =>
                [
                    new(new(new("root::main"), new(functionType))),
                    new(new(new("root::helper"), new(functionType))),
                    new(new(new("root::internal"), new(namespaceType))),
                ],
                "root::internal" =>
                [
                    new(new(new("root::internal::compute"), new(functionType))),
                ],
                "dependency" =>
                [
                    new(new(new("dependency::external_function"), new(functionType))),
                ],
                _ => []
            }))
            .AddHierarchyLinks(new(containsType), new(functionType), new(operationType), sourceId => new(sourceId.Value switch
            {
                "root::main" =>
                [
                    new(new(new("root::main::assignment1"), new(assignmentOperationType))),
                    new(new(new("root::main::call"), new(invocationOperationType))),
                    new(new(new("root::main::assignment2"), new(assignmentOperationType))),
                ],
                "root::helper" =>
                [
                    new(new(new("root::helper::call1"), new(invocationOperationType))),
                    new(new(new("root::helper::call2"), new(invocationOperationType))),
                    new(new(new("root::helper::call3"), new(invocationOperationType))),
                ],
                "root::internal::compute" =>
                [
                    new(new(new("root::internal::compute::assignment1"), new(assignmentOperationType))),
                    new(new(new("root::internal::compute::assignment2"), new(assignmentOperationType))),
                ],
                "dependency::external_function" =>
                [
                    new(new(new("dependency::external_function::assignment"), new(assignmentOperationType))),
                ],
                _ => []
            }))
            .AddLinks(new(isFollowedByType), new(operationType), new(operationType), sourceId => new(sourceId.Value switch
            {
                "root::main::assignment1" => new(new(new("root::main::call"), new(invocationOperationType))),
                "root::main::call" => new(new(new("root::main::assignment2"), new(assignmentOperationType))),
                "root::helper::call1" => new(new(new("root::helper::call2"), new(invocationOperationType))),
                "root::helper::call2" => new(new(new("root::helper::call3"), new(invocationOperationType))),
                "root::internal::compute::assignment1" => new(new(new("root::internal::compute::assignment2"), new(assignmentOperationType))),
                _ => (ISliceBuilder.PartialLinkInfo?)null
            }))
            .AddLinks(new(callsType), new(invocationOperationType), new(functionType), sourceId => new(sourceId.Value switch
            {
                "root::main::call" => new(new(new("root::helper"))),
                "root::helper::call1" => new(new(new("root::internal::compute"))),
                "root::helper::call2" => new(new(new("dependency::external_function"))),
                "root::helper::call3" => new(new(new("dependency::external_function"))),
                _ => (ISliceBuilder.PartialLinkInfo?) null
            }))
            .BuildLazy();
    }

    public Task<IModel> InitAsync()
    {
        return DisplayCurrentLevelAsync();
    }

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _openActionName && command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            _selectedElementId = new(id);

            return await DisplayCurrentLevelAsync();
        }
        else
        {
            return null;
        }
    }

    private async Task<IModel> DisplayCurrentLevelAsync()
    {
        ElementInfo[] elements;
        if (_selectedElementId is ElementId selectedElementId)
        {
            if (_slice.Schema.HierarchyLinkType is LinkType hierarchyType)
            {
                var hierarchyExplorer = _slice.GetLinkExplorer(hierarchyType);
                elements = [.. await hierarchyExplorer.GetTargetElementsAsync(selectedElementId)];
            }
            else
            {
                elements = [];
            }
        }
        else
        {
            elements = [.. await _slice.GetRootElementsAsync()];
        }

        var nameProvider = _slice.GetElementAttributeProviderAsyncCallback("Name");

        var externalElements = new List<ElementInfo>();

        var nodes = new List<Node>();
        var edges = new List<Edge>();
        foreach (var element in elements)
        {
            var name = await nameProvider(element.Id);

            nodes.Add(new(
                element.Id.Value,
                name,
                CreateOpenCommand(element)));

            foreach (var kvp in _slice.Schema.LinkTypes)
            {
                var (linkType, elementTypes) = (kvp.Key, kvp.Value);

                if (linkType == _slice.Schema.HierarchyLinkType)
                {
                    continue;
                }

                if (elementTypes.Any(t => t.SourceType.Value.IsSupersetOfOrEquals(element.Type.Value)))
                {
                    var linkExplorer = _slice.GetLinkExplorer(linkType);
                    foreach (var targetElement in await linkExplorer.GetTargetElementsAsync(element.Id))
                    {
                        edges.Add(new(
                            element.Id.Value,
                            targetElement.Id.Value,
                            TryGetLinkTypeLabel(linkType)));

                        if (!elements.Contains(targetElement) && !externalElements.Contains(targetElement))
                        {
                            nodes.Add(new(
                                targetElement.Id.Value,
                                "external: " + await nameProvider(targetElement.Id),
                                CreateOpenCommand(targetElement)));

                            externalElements.Add(targetElement);
                        }
                    }
                }
            }
        }

        return new Graph([.. nodes], [.. edges]);
    }

    private string? TryGetLinkTypeLabel(LinkType linkType)
    {
        if (linkType.Value.AttributeValues.TryGetValue("Kind", out var kinds) && kinds.Count() == 1)
        {
            return kinds.Single();
        }
        else
        {
            return null;
        }
    }

    private static Command CreateOpenCommand(ElementInfo element)
    {
        return new(_openActionName, ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }
}
