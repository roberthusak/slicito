using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;

namespace Slicito.DotNet.Tests;

[TestClass]
public class DotNetFactProviderTest
{
    private const string _solutionPath = @"..\..\..\..\inputs\SampleSolution\SampleSolution.sln";

    [TestMethod]
    public async Task BasicSampleSolutionStructureTest()
    {
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

        var result = await provider.QueryAsync(query);

        result.Should().NotBeNull();
        result.Elements.Should().NotBeNull();
        result.Relations.Should().NotBeNull();

        result.Elements.Should().ContainSingle(e => e.Kind == DotNetElementKind.Solution);
        result.Elements.Where(e => e.Kind == DotNetElementKind.Project).Should().HaveCount(3);

        result.Relations.Should().ContainSingle().Which.Links.Should().HaveCount(3);
    }
}
