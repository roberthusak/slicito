using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;

namespace StandaloneGui;

public class DotNetTreeController : IController
{
    private const string _openActionName = "open";
    private const string _idActionParameterName = "id";

    private readonly DotNetFactProvider _factProvider;

    public DotNetTreeController(DotNetFactProvider factProvider)
    {
        _factProvider = factProvider;
    }

    public async Task<IModel> Init()
    {
        var facts = await GatherFacts();
        var projects = facts.Elements.Where(e => e.Kind == DotNetElementKind.Project);

        return CreateModel(projects);
    }

    public async Task<IModel?> ProcessCommand(Command command)
    {
        if (command.Name == _openActionName && command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            var facts = await GatherFacts();
            var nestedFacts = facts.Elements.Where(e => e.Id.StartsWith(id) && e.Id != id);

            return CreateModel(nestedFacts);
        }
        else
        {
            return null;
        }
    }

    private async Task<FactQueryResult> GatherFacts()
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

        return await _factProvider.QueryAsync(query);
    }

    private static Tree CreateModel(IEnumerable<IElement> elements)
    {
        return new Tree(
            elements
            .Select(e =>
                new TreeItem(
                    e.Id,
                    [],
                    new Command(
                        _openActionName,
                        ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, e.Id))))
            .ToImmutableArray());
    }
}
