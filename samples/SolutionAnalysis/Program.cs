using Slicito.Abstractions.Relations;
using Slicito.DotNet;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

using SolutionAnalysis;

var globalContext = await new DotNetContext.Builder()
    .AddProject(args[0])
    .BuildAsync();

var dependencyRelations = globalContext.ExtractDependencyRelations();

var namespaceDependsOnRelation = Relation.Merge(dependencyRelations)
    .MoveUpHierarchy(globalContext.Hierarchy, (_, hierarchyPair) =>
        hierarchyPair.Target is not DotNetNamespace)
    .MakeUnique()
    .Filter(pair =>
        pair.Source != pair.Target
        && !globalContext.Hierarchy.GetAncestors(pair.Source).Contains(pair.Target)
        && !globalContext.Hierarchy.GetAncestors(pair.Target).Contains(pair.Source));

var schema = new Schema.Builder()
    .AddUriProvider(globalContext.OpenInIdeUriProvider)
    .AddNodes(
        globalContext.Elements.Where(e => e is not DotNetType and not DotNetTypeMember),
        globalContext.Hierarchy)
    .AddEdges(namespaceDependsOnRelation)
    .BuildSvg();

var uri = await schema.UploadToServerAsync();
Utils.OpenUri(uri);
