using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;

namespace StandaloneGui;

internal class DotNetTreeController : IController
{
    private readonly DotNetFactProvider _factProvider;

    public DotNetTreeController(DotNetFactProvider factProvider)
    {
        _factProvider = factProvider;
    }

    public async Task<IModel> Init()
    {
        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: true),
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: false)
            ]);

        var result = await _factProvider.QueryAsync(query);

        return new Tree(
            result
            .Elements
            .Select(e =>
                new TreeItem(
                    e.Id,
                    []))
            .ToImmutableArray());
    }
}
