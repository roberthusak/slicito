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

        var solutionRequirement =
            query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Solution);
        var solutionContainsRequirement =
            query.RelationRequirements.FirstOrDefault(r => r.Kind == DotNetRelationKind.SolutionContains);
        var projectRequirement =
            query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Project);

        if (solutionRequirement is not null)
        {
            var solutionElement = new DotNetElement(
                DotNetElementKind.Solution,
                id: _solution.FilePath!,
                name: Path.GetFileNameWithoutExtension(_solution.FilePath!));

            if (solutionRequirement.Filter is null || solutionRequirement.Filter(solutionElement))
            {
                elements.Add(solutionElement);

                if (solutionContainsRequirement is not null && projectRequirement is not null)
                {
                    foreach (var project in _solution.Projects)
                    {
                        var projectElement = new DotNetElement(
                            DotNetElementKind.Project,
                            id: project.FilePath!,
                            name: project.Name);

                        if (projectRequirement.Filter is not null && !projectRequirement.Filter(projectElement))
                        {
                            continue;
                        }

                        elements.Add(projectElement);

                        var solutionProjectLink = new DotNetLink(
                            source: solutionElement,
                            target: projectElement);

                        solutionContains.Add(solutionProjectLink);
                    }
                }
            }
        }

        var solutionContainsRelation = new DotNetRelation(DotNetRelationKind.SolutionContains, [.. solutionContains]);

        var result = new FactQueryResult(elements, [solutionContainsRelation]);
        return Task.FromResult(result);
    }
}
