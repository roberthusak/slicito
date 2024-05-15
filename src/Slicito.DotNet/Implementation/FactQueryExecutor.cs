using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal partial class FactQueryExecutor
{
    private readonly KnownRequirements _requirements;
    private readonly List<DotNetElement> _elements = [];
    private readonly List<DotNetLink> _solutionContainsLinks = [];

    public static FactQueryResult ExecuteQuery(Solution solution, FactQuery query)
    {
        var executor = new FactQueryExecutor(query);

        return executor.ExecuteQuery(solution);
    }

    private FactQueryExecutor(FactQuery query)
    {
        _requirements = new KnownRequirements(query);
    }

    private FactQueryResult ExecuteQuery(Solution solution)
    {
        ProcessSolution(solution);

        return CreateResult();
    }

    private void ProcessSolution(Solution solution)
    {
        if (_requirements.Solution is null)
        {
            return;
        }

        var element = new DotNetElement(
            DotNetElementKind.Solution,
            solution.FilePath!,
            Path.GetFileNameWithoutExtension(solution.FilePath!));

        if (_requirements.Solution.Filter is not null && !_requirements.Solution.Filter(element))
        {
            return;
        }

        _elements.Add(element);

        ProcessSolutionContains(solution, element);
    }

    private void ProcessSolutionContains(Solution solution, DotNetElement solutionElement)
    {
        if (_requirements.SolutionContains is null || _requirements.Project is null)
        {
            return;
        }

        foreach (var project in solution.Projects)
        {
            ProcessProject(solutionElement, project);
        }
    }

    private void ProcessProject(DotNetElement solutionElement, Project project)
    {
        var projectElement = new DotNetElement(
            DotNetElementKind.Project,
            project.FilePath!,
            project.Name);

        if (_requirements.Project?.Filter is not null && !_requirements.Project.Filter(projectElement))
        {
            return;
        }

        var solutionProjectLink = new DotNetLink(
            source: solutionElement,
            target: projectElement);

        if (_requirements.SolutionContains?.Filter is not null && !_requirements.SolutionContains.Filter(solutionProjectLink))
        {
            return;
        }

        _elements.Add(projectElement);
        _solutionContainsLinks.Add(solutionProjectLink);
    }

    private FactQueryResult CreateResult()
    {
        var relations = new List<DotNetRelation>();

        if (_requirements.SolutionContains is not null)
        {
            relations.Add(new DotNetRelation(DotNetRelationKind.SolutionContains, _solutionContainsLinks));
        }   

        return new FactQueryResult(_elements, relations);
    }
}
