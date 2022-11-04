using Slicito.Abstractions.Relations;
using Slicito.DotNet;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

using SolutionAnalysis;

var globalContext = await new DotNetContext.Builder()
    .AddSolution(args[0])
    .BuildAsync();

var dependencyRelations = globalContext.ExtractDependencyRelations();

var namespaceDependsOnRelation = Relation.Merge(dependencyRelations)
    .MoveUpHierarchy(globalContext.Hierarchy, (_, hierarchyPair) =>
        hierarchyPair.Target is DotNetTypeMember or DotNetType)
    .MakeUnique()
    .Filter(pair =>
        pair.Source != pair.Target
        && !globalContext.Hierarchy.GetAncestors(pair.Source).Contains(pair.Target)
        && !globalContext.Hierarchy.GetAncestors(pair.Target).Contains(pair.Source));

var compactedHierarchy = globalContext.Hierarchy
    .Filter(pair => pair.Target is not DotNetTypeMember)
    .CompactPaths(pair => pair.Target is DotNetNamespace);

var schema = new Schema.Builder()
    .AddLabelProvider(globalContext.LabelProvider)
    .AddUriProvider(globalContext.OpenInIdeUriProvider)
    .AddNodes(compactedHierarchy)
    .AddEdges(namespaceDependsOnRelation)
    .BuildSvg();

var uri = await schema.UploadToServerAsync();
Utils.OpenUri(uri);
