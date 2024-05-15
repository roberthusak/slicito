using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;

namespace Slicito.DotNet.Tests;

[TestClass]
public class DotNetFactProviderTest
{
    private const string _solutionPath = @"..\..\..\..\inputs\SampleSolution\SampleSolution.sln";

    [TestMethod]
    public async Task Provides_SolutionAndProjects()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, returnAll: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, returnAll: true)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project).Should().HaveCount(3);

        result.Relations.Should().ContainSingle().Which.Links.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Provides_SolutionAndProjects_WithElementFilter()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, returnAll: true, filter: element =>
                    (element as DotNetElement)?.Name != "TestProject")
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, returnAll: true)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project)
            .Should().HaveCount(2)
            .And.NotContain(element => element.As<DotNetElement>().Name == "TestProject");

        result.Relations.Should().ContainSingle().Which.Links.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Provides_SolutionAndProjects_WithRelationFilter()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, returnAll: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, returnAll: true, filter: link =>
                    (link.Target as DotNetElement)?.Name != "TestProject")
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project)
            .Should().HaveCount(2)
            .And.NotContain(element => element.As<DotNetElement>().Name == "TestProject");

        result.Relations.Should().ContainSingle().Which.Links.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Provides_SolutionProjectsNamespacesAndTypes()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Type, returnAll: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, returnAll: true),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, returnAll: true),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, returnAll: true)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project).Should().HaveCount(3);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Namespace).Should().HaveCount(4);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Type).Should().HaveCount(5);

        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.SolutionContains)
            .Which.Links.Should().HaveCount(3);
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.ProjectContains)
            .Which.Links.Should().HaveCount(3);
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.NamespaceContains)
            .Which.Links.Should().HaveCount(6)
            .And.ContainSingle(l => l.Source.Kind == DotNetElementKind.Namespace && l.Target.Kind == DotNetElementKind.Namespace);
    }

    [TestMethod]
    public async Task Provides_SolutionProjectsNamespacesAndTypes_WithElementFilters()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, returnAll: true),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, returnAll: true, filter: element =>
                    (element as DotNetElement)?.Name != "Implementation"),
                new FactQueryElementRequirement(DotNetElementKind.Type, returnAll: true, filter: element =>
                    (element as DotNetElement)?.Name != "Program")
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, returnAll: true),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, returnAll: true),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, returnAll: true)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project).Should().HaveCount(3);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Namespace).Should().HaveCount(3);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Type).Should().HaveCount(3);

        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.SolutionContains)
            .Which.Links.Should().HaveCount(3);
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.ProjectContains)
            .Which.Links.Should().HaveCount(3);
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.NamespaceContains)
            .Which.Links.Should().HaveCount(3)
            .And.NotContain(l => l.Source.Kind == DotNetElementKind.Namespace && l.Target.Kind == DotNetElementKind.Namespace);
    }
}
