using System.Collections.Immutable;

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
            .AddHierarchyLinks(new(containsType), new(namespaceType), new(functionType), sourceId => new(sourceId.Value switch
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
                    new(new(new("root::main::assignment"), new(assignmentOperationType))),
                    new(new(new("root::main::call"), new(invocationOperationType))),
                ],
                "root::helper" =>
                [
                    new(new(new("root::helper::call"), new(invocationOperationType))),
                ],
                "root::internal::compute" =>
                [
                    new(new(new("root::internal::compute::assignment"), new(assignmentOperationType))),
                ],
                "dependency::external_function" =>
                [
                    new(new(new("dependency::external_function::call"), new(invocationOperationType))),
                ],
                _ => []
            }))
            .BuildLazy();
    }

    public Task<IModel> Init()
    {
        return DisplayCurrentLevelAsync();
    }

    public async Task<IModel?> ProcessCommand(Command command)
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

        var nodes = new List<Node>();
        foreach (var element in elements)
        {
            var name = await nameProvider(element.Id);

            nodes.Add(new(
                element.Id.Value,
                name,
                CreateOpenCommand(element)));
        }

        return new Graph([.. nodes], []);
    }

    private static Command CreateOpenCommand(ElementInfo element)
    {
        return new(_openActionName, ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }
}
