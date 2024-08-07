using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;

namespace StandaloneGui;

public class SampleDotNetController : IController
{
    private const string _openActionName = "open";
    private const string _idActionParameterName = "id";

    private readonly DotNetFactProvider _factProvider;

    public SampleDotNetController(DotNetFactProvider factProvider)
    {
        _factProvider = factProvider;
    }

    public async Task<IModel> Init()
    {
        var facts = await GatherFacts();
        var projects = facts.Elements.Where(e => e.Kind == DotNetElementKind.Project);

        return CreateList(projects);
    }

    public async Task<IModel?> ProcessCommand(Command command)
    {
        if (command.Name == _openActionName && command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            var facts = await GatherFacts();
            var nestedFacts = facts.Elements.Where(e => e.Id.StartsWith(id));

            return CreateGraph(nestedFacts, facts.Relations);
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

    private static Tree CreateList(IEnumerable<IElement> elements)
    {
        return new Tree(
            elements
            .Select(e =>
                new TreeItem(
                    e.Id,
                    [],
                    CreateOpenCommand(e.Id)))
            .ToImmutableArray());
    }

    private static Graph CreateGraph(IEnumerable<IElement> elements, IEnumerable<IRelation> relations)
    {
        var elementsSet = elements.ToHashSet();

        var nodes = elementsSet
            .Select(e => new Node(
                e.Id,
                (e as DotNetElement)?.Name ?? e.Id,
                CreateOpenCommand(e.Id)))
            .ToImmutableArray();

        var edges = relations
            .SelectMany(r =>
                r.Links
                .Where(l => elementsSet.Contains(l.Source) && elementsSet.Contains(l.Target))
                .Select(l => new Edge(l.Source.Id, l.Target.Id, r.Kind.Name, null)))
            .ToImmutableArray();

        return new Graph(nodes, edges);
    }

    private static Command CreateOpenCommand(string id)
    {
        return new Command(_openActionName, ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, id));
    }
}
