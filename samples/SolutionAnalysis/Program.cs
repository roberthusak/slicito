using System.Diagnostics;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;
using Slicito.DotNet;

var solutionStopwatch = Stopwatch.StartNew();

var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(args[0]);

Console.WriteLine($"Solution opened in {solutionStopwatch.Elapsed.TotalMilliseconds} ms");

var provider = new DotNetFactProvider(solution);

var query = new FactQuery(
    [
        new FactQueryElementRequirement(DotNetElementKind.Solution, includeChildless: false),
        new FactQueryElementRequirement(DotNetElementKind.Project, includeChildless: false),
        new FactQueryElementRequirement(DotNetElementKind.Namespace, includeChildless: false),
        new FactQueryElementRequirement(DotNetElementKind.Type, includeChildless: true),
    ],
    [
        new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, includeChildless: false),
        new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, includeChildless: false),
        new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, includeChildless: false)
    ]);

var queryStopwatch = Stopwatch.StartNew();

var result = await provider.QueryAsync(query);

Console.WriteLine($"Query completed in {queryStopwatch.Elapsed.TotalMilliseconds} ms");

Console.WriteLine(
    "Projects: {0}",
    result.Elements.Count(e => e.Kind == DotNetElementKind.Project));
Console.WriteLine(
    "Namespaces: {0}",
    result.Elements.Count(e => e.Kind == DotNetElementKind.Namespace));
Console.WriteLine(
    "Types: {0}",
    result.Elements.Count(e => e.Kind == DotNetElementKind.Type));

Console.WriteLine(
    "SolutionContains links: {0}",
    result.Relations.Single(l => l.Kind == DotNetRelationKind.SolutionContains).Links.Count());

Console.WriteLine(
    "ProjectContains links: {0}",
    result.Relations.Single(l => l.Kind == DotNetRelationKind.ProjectContains).Links.Count());

Console.WriteLine(
    "NamespaceContains links: {0}",
    result.Relations.Single(l => l.Kind == DotNetRelationKind.NamespaceContains).Links.Count());
