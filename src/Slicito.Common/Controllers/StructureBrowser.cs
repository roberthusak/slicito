using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Abstractions.Queries;

namespace Slicito.Common.Controllers;

public class StructureBrowser(ILazySlice slice) : IController
{
    private const string _openActionName = "Open";
    private const string _idActionParameterName = "Id";
    private ElementId? _selectedElementId;

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
            if (slice.Schema.HierarchyLinkType is LinkType hierarchyType)
            {
                var hierarchyExplorer = slice.GetLinkExplorer(hierarchyType);
                elements = [.. await hierarchyExplorer.GetTargetElementsAsync(selectedElementId)];
            }
            else
            {
                elements = [];
            }
        }
        else
        {
            elements = [.. await slice.GetRootElementsAsync()];
        }

        var nameProvider = slice.GetElementAttributeProviderAsyncCallback("Name");

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

            foreach (var kvp in slice.Schema.LinkTypes)
            {
                var (linkType, elementTypes) = (kvp.Key, kvp.Value);

                if (linkType == slice.Schema.HierarchyLinkType)
                {
                    continue;
                }

                if (elementTypes.Any(t => t.SourceType.Value.IsSupersetOfOrEquals(element.Type.Value)))
                {
                    var linkExplorer = slice.GetLinkExplorer(linkType);
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

    private static string? TryGetLinkTypeLabel(LinkType linkType)
    {
        if (linkType.Value.AttributeValues.TryGetValue("Kind", out var kinds) && kinds.Count == 1)
        {
            return kinds[0];
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
