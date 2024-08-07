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
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: true)
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
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: true, Filter: element =>
                    (element as DotNetElement)?.Name != "TestProject")
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: true)
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
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: true, Filter: link =>
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
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: true)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: true),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: true),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: true)
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
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: true),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: true, Filter: element =>
                    (element as DotNetElement)?.Name != "Implementation"),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: true, Filter: element =>
                    (element as DotNetElement)?.Name != "Program")
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: true),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: true),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: true)
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

    [TestMethod]
    public async Task Provides_SolutionProjectsNamespacesAndTypes_WithElementFilters_WithoutChildlessElements()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: false, Filter: element =>
                    (element as DotNetElement)?.Name != "ConsoleProject"),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: true, Filter: element =>
                    (element as DotNetElement)?.Name != "Adder" && (element as DotNetElement)?.Name != "AdderTest")
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: false)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Project)
            .Which.As<DotNetElement>().Name.Should().Be("LibraryProject");
        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Namespace)
            .Which.As<DotNetElement>().Name.Should().Be("LibraryProject");
        result.Elements.Where(e => e.Kind == DotNetElementKind.Type).Should().HaveCount(2);

        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.SolutionContains)
            .Which.Links.Should().ContainSingle();
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.ProjectContains)
            .Which.Links.Should().ContainSingle();
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.NamespaceContains)
            .Which.Links.Should().HaveCount(2)
            .And.NotContain(l => l.Source.Kind == DotNetElementKind.Namespace && l.Target.Kind == DotNetElementKind.Namespace);
    }

    [TestMethod]
    public async Task Returns_EmptyResult_WhenAllElementsChildless()
    {
        // Arrange

        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: false)
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: false)
            ]);

        // Act

        var result = await provider.QueryAsync(query);

        // Assert

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().BeEmpty();

        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.SolutionContains)
            .Which.Links.Should().BeEmpty();
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.ProjectContains)
            .Which.Links.Should().BeEmpty();
        result.Relations.Should().ContainSingle(r => r.Kind == DotNetRelationKind.NamespaceContains)
            .Which.Links.Should().BeEmpty();
    }
}
