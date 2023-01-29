using Slicito.Abstractions;
using Slicito.DotNet;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

using SolutionAnalysis;

var globalContext = await new DotNetContext.Builder()
    .AddSolution(args[0])
    .BuildAsync();

var dependencyRelations = globalContext.ExtractDependencyRelations();
var dataFlowRelations = globalContext.ExtractDataFlowRelations(dependencyRelations);
var controlFlowRelations = globalContext.ExtractControlFlowRelations(dependencyRelations);

var namespaceDependsOnRelation = Relation.Merge(dependencyRelations)
    .MoveUpHierarchy(globalContext.Hierarchy, (_, hierarchyPair) =>
        hierarchyPair.Target is DotNetOperation or DotNetBlock or DotNetVariable or DotNetTypeMember or DotNetType)
    .MakeUnique()
    .Filter(pair =>
        pair.Source != pair.Target
        && !globalContext.Hierarchy.GetAncestors(pair.Source).Contains(pair.Target)
        && !globalContext.Hierarchy.GetAncestors(pair.Target).Contains(pair.Source));

var compactedHierarchy = globalContext.Hierarchy
    .Filter(pair => pair.Target is not DotNetTypeMember and not DotNetOperation and not DotNetBlock and not DotNetVariable)
    .CompactPaths(pair => pair.Target is DotNetNamespace);

var schema = new Schema.Builder()
    .AddLabelProvider(globalContext.LabelProvider)
    .AddNodes(compactedHierarchy)
    .AddEdges(namespaceDependsOnRelation)
    .Build();

Utils.SaveSvgAndOpen(schema);
