using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.DataFlow;
using Slicito.ProgramAnalysis.DataFlow.Analyses;

using System.Collections.Immutable;

namespace Controllers;

public class DotNetMethodBrowser : IController
{
    private const string _openActionName = "Open";
    private const string _analyzeActionName = "Analyze";

    private const string _idActionParameterName = "Id";
    private const string _analysisKindParameterName = "AnalysisKind";

    private const string _reachingDefinitionsAnalysisKind = "ReachingDefinitions";

    private readonly DotNetSolutionContext _solutionContext;
    private readonly DotNetTypes _dotNetTypes;

    private readonly ILazySlice _slice;

    public DotNetMethodBrowser(DotNetSolutionContext solutionContext, DotNetTypes dotNetTypes)
    {
        _solutionContext = solutionContext;
        _dotNetTypes = dotNetTypes;

        _slice = solutionContext.LazySlice;
    }

    public async Task<IModel> InitAsync()
    {
        return await DisplayMethodListAsync();
    }

    public Task<IModel?> ProcessCommandAsync(Command command)
    {
        IModel? result = null;

        if (command.Name == _openActionName && 
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            result = DisplayFlowGraph(new ElementId(id));
        }
        else if (command.Name == _analyzeActionName && 
            command.Parameters.TryGetValue(_idActionParameterName, out var id2) &&
            command.Parameters.TryGetValue(_analysisKindParameterName, out var analysisKind) &&
            analysisKind == _reachingDefinitionsAnalysisKind)
        {
            result = DisplayReachingDefinitionsAnalysis(new ElementId(id2));
        }

        return Task.FromResult(result);
    }

    private Graph DisplayFlowGraph(ElementId id)
    {
        var flowGraph = _solutionContext.TryGetFlowGraph(id)
            ?? throw new Exception($"Flow graph for method {id} not found");

        var result = FlowGraphHelper.CreateGraphModel(flowGraph);

        return result;
    }

    private Graph DisplayReachingDefinitionsAnalysis(ElementId id)
    {
        var flowGraph = _solutionContext.TryGetFlowGraph(id)
            ?? throw new Exception($"Flow graph for method {id} not found");

        var reachingDefinitions = ReachingDefinitions.Create();
        var result = AnalysisExecutor.Execute(flowGraph, reachingDefinitions);
        var defUses = ReachingDefinitions.GetDefUses(result);

        var additionalEdges = defUses.Select(defUse => new FlowGraphHelper.AdditionalEdge(
            defUse.Definition.Block,
            defUse.Use.Block,
            "is used in"));

        var graph = FlowGraphHelper.CreateGraphModel(flowGraph, additionalEdges);

        return graph;
    }

    private async Task<IModel> DisplayMethodListAsync()
    {
        var nodes = new List<Node>();
        var edges = new List<Edge>();

        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_slice, _dotNetTypes);
        
        foreach ((var method, var displayName) in methods)
        {
            nodes.Add(new Node(
                method.Id.Value,
                displayName,
                CreateOpenCommand(method)));

            nodes.Add(new Node(
                $"{method.Id.Value}-{_reachingDefinitionsAnalysisKind}",
                $"{displayName} - {_reachingDefinitionsAnalysisKind}",
                CreateAnalyzeCommand(method, _reachingDefinitionsAnalysisKind)));
        }

        return new Graph([.. nodes], [.. edges]);
    }

    private static Command CreateOpenCommand(ElementInfo element)
    {
        return new Command(
            _openActionName, 
            ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }

    private static Command CreateAnalyzeCommand(ElementInfo element, string analysisKind)
    {
        return new Command(
            _analyzeActionName,
            ImmutableDictionary<string, string>.Empty
                .Add(_idActionParameterName, element.Id.Value)
                .Add(_analysisKindParameterName, analysisKind));
    }
}
