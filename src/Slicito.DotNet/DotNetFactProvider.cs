using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetFactProvider : IFactProvider
{
    private readonly Solution _solution;

    public DotNetFactProvider(Solution solution)
    {
        _solution = solution;
    }

    public Task<FactQueryResult> QueryAsync(FactQuery query)
    {
        var elements = new List<DotNetElement>();
        var solutionContains = new List<DotNetLink>();

        if (query.ElementRequirements.Any(r => r.Kind == DotNetElementKind.Solution))
        {
            var solutionElement = new DotNetElement(
                DotNetElementKind.Solution,
                id: _solution.FilePath!,
                name: Path.GetFileNameWithoutExtension(_solution.FilePath!));

            elements.Add(solutionElement);

            if (query.RelationRequirements.Any(r => r.Kind == DotNetRelationKind.SolutionContains))
            {
                foreach (var project in _solution.Projects)
                {
                    var projectElement = new DotNetElement(
                        DotNetElementKind.Project,
                        id: project.FilePath!,
                        name: project.Name);

                    elements.Add(projectElement);

                    var solutionProjectLink = new DotNetLink(
                        source: solutionElement,
                        target: projectElement);

                    solutionContains.Add(solutionProjectLink);
                }
            }
        }

        var solutionContainsRelation = new DotNetRelation(DotNetRelationKind.SolutionContains, [.. solutionContains]);

        var result = new FactQueryResult(elements, [solutionContainsRelation]);
        return Task.FromResult(result);
    }
}
