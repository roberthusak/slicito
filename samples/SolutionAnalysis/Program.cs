using Microsoft.Msagl.Drawing;

using Slicito;
using Slicito.Abstractions.Relations;
using Slicito.DotNet;
using Slicito.DotNet.Elements;

using SolutionAnalysis;

var globalContext = await new DotNetContext.Builder()
    .AddProject(args[0])
    .BuildAsync();

var dependencyRelations = globalContext.ExtractDependencyRelations();
var dependsOnRelation = Relation.Merge(dependencyRelations);
var typeDependsOnRelation = dependsOnRelation
    .MoveUpHierarchy(globalContext.Hierarchy, (_, hierarchyPair) =>
        hierarchyPair.Target is not DotNetType and not DotNetNamespace)
    .MakeUnique();

var graph = new Graph();

foreach (var element in globalContext.Elements.OfType<DotNetType>())
{
    graph.AddNode(element.Id);
}

foreach (var pair in typeDependsOnRelation.Pairs)
{
    graph.AddEdge(pair.Source.Id, pair.Target.Id);
}

var uri = await graph.RenderToSvgUriAsync();
Utils.OpenUri(uri);
