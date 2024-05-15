using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal partial class FactQueryExecutor
{
    private readonly KnownRequirements _requirements;
    private readonly List<DotNetElement> _elements = [];
    private readonly List<DotNetLink> _solutionContainsLinks = [];
    private readonly List<DotNetLink> _projectContainsLinks = [];
    private readonly List<DotNetLink> _namespaceContainsLinks = [];

    public static async Task<FactQueryResult> ExecuteQuery(Solution solution, FactQuery query)
    {
        var executor = new FactQueryExecutor(query);

        return await executor.ExecuteQuery(solution);
    }

    private FactQueryExecutor(FactQuery query)
    {
        _requirements = new KnownRequirements(query);
    }

    private async Task<FactQueryResult> ExecuteQuery(Solution solution)
    {
        await ProcessSolution(solution);

        return CreateResult();
    }

    private async Task ProcessSolution(Solution solution)
    {
        if (_requirements.Solution is null)
        {
            return;
        }

        var solutionElement = new DotNetElement(
            DotNetElementKind.Solution,
            id: solution.FilePath!,
            name: Path.GetFileNameWithoutExtension(solution.FilePath!));

        if (_requirements.Solution.Filter is not null && !_requirements.Solution.Filter(solutionElement))
        {
            return;
        }

        var anyChild = await ProcessSolutionContains(solution, solutionElement);

        if (!anyChild && !_requirements.Solution.IncludeChildless)
        {
            return;
        }

        _elements.Add(solutionElement);
    }

    private async Task<bool> ProcessSolutionContains(Solution solution, DotNetElement solutionElement)
    {
        if (_requirements.SolutionContains is null || _requirements.Project is null)
        {
            return false;
        }

        var anyChild = false;

        foreach (var project in solution.Projects)
        {
            anyChild = await ProcessProject(solutionElement, project) || anyChild;
        }

        return anyChild;
    }

    private async Task<bool> ProcessProject(DotNetElement solutionElement, Project project)
    {
        var projectElement = new DotNetElement(
            DotNetElementKind.Project,
            id: project.FilePath!,
            name: project.Name);

        if (_requirements.Project?.Filter is not null && !_requirements.Project.Filter(projectElement))
        {
            return false;
        }

        var solutionProjectLink = new DotNetLink(
            source: solutionElement,
            target: projectElement);

        if (_requirements.SolutionContains?.Filter is not null && !_requirements.SolutionContains.Filter(solutionProjectLink))
        {
            return false;
        }

        var anyChild = await ProcessProjectContains(project, projectElement);

        if (!anyChild && !_requirements.Project!.IncludeChildless)
        {
            return false;
        }

        _elements.Add(projectElement);
        _solutionContainsLinks.Add(solutionProjectLink);

        return true;
    }

    private async Task<bool> ProcessProjectContains(Project project, DotNetElement projectElement)
    {
        if (_requirements.ProjectContains is null || _requirements.Namespace is null)
        {
            return false;
        }

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");

        var anyChild = false;

        foreach (var @namespace in compilation.SourceModule.GlobalNamespace.GetMembers().OfType<INamespaceSymbol>())
        {
            anyChild = ProcessNamespace(projectElement, @namespace) || anyChild;
        }

        return anyChild;
    }

    private bool ProcessNamespace(DotNetElement containingElement, INamespaceSymbol @namespace)
    {
        var namespaceElement = new DotNetElement(
            DotNetElementKind.Namespace,
            id: $"{containingElement.Id}.{@namespace.Name}",
            name: @namespace.Name);

        if (_requirements.Namespace?.Filter is not null && !_requirements.Namespace.Filter(namespaceElement))
        {
            return false;
        }

        var containsLink = new DotNetLink(
            source: containingElement,
            target: namespaceElement);

        var (filter, links) = containingElement.Kind.Name switch
        {
            DotNetElementKindNames.Namespace => (_requirements.NamespaceContains?.Filter, _namespaceContainsLinks),
            DotNetElementKindNames.Project => (_requirements.ProjectContains?.Filter, _projectContainsLinks),
            _ => throw new InvalidOperationException($"Unexpected containing element kind: {containingElement.Kind.Name}")
        };

        if (filter is not null && !filter(containsLink))
        {
            return false;
        }

        var anyChild = ProcessNamespaceContains(@namespace, namespaceElement);

        if (!anyChild && !_requirements.Namespace!.IncludeChildless)
        {
            return false;
        }

        _elements.Add(namespaceElement);
        links.Add(containsLink);

        return true;
    }

    private bool ProcessNamespaceContains(INamespaceSymbol @namespace, DotNetElement namespaceElement)
    {
        if (_requirements.NamespaceContains is null)
        {
            return false;
        }

        var anyChild = false;

        foreach (var member in @namespace.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol nestedNamespace:
                    anyChild = ProcessNamespace(namespaceElement, nestedNamespace) || anyChild;
                    break;

                case ITypeSymbol type:
                    anyChild = ProcessType(namespaceElement, type) || anyChild;
                    break;
            }
        }

        return anyChild;
    }

    private bool ProcessType(DotNetElement namespaceElement, ITypeSymbol type)
    {
        if (_requirements.Type is null)
        {
            return false;
        }

        var typeElement = new DotNetElement(
            DotNetElementKind.Type,
            id: $"{namespaceElement.Id}.{type.Name}",
            name: type.Name);

        if (_requirements.Type.Filter is not null && !_requirements.Type.Filter(typeElement))
        {
            return false;
        }

        var namespaceTypeLink = new DotNetLink(
            source: namespaceElement,
            target: typeElement);

        if (_requirements.NamespaceContains!.Filter is not null && !_requirements.NamespaceContains.Filter(namespaceTypeLink))
        {
            return false;
        }

        if (!_requirements.Type.IncludeChildless)
        {
            return false;
        }

        _elements.Add(typeElement);
        _namespaceContainsLinks.Add(namespaceTypeLink);

        return true;
    }

    private FactQueryResult CreateResult()
    {
        var relations = new List<DotNetRelation>();

        if (_requirements.SolutionContains is not null)
        {
            relations.Add(new DotNetRelation(DotNetRelationKind.SolutionContains, _solutionContainsLinks));
        }

        if (_requirements.ProjectContains is not null)
        {
            relations.Add(new DotNetRelation(DotNetRelationKind.ProjectContains, _projectContainsLinks));
        }

        if (_requirements.NamespaceContains is not null)
        {
            relations.Add(new DotNetRelation(DotNetRelationKind.NamespaceContains, _namespaceContainsLinks));
        }

        return new FactQueryResult(_elements, relations);
    }
}
