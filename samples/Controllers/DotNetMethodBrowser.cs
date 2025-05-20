using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.Abstractions.Models;
using Slicito.Common.Controllers;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.DataFlow;
using Slicito.ProgramAnalysis.DataFlow.Analyses;
using Slicito.ProgramAnalysis.Interprocedural;

using System.Collections.Immutable;

namespace Controllers;

public class DotNetMethodBrowser : IController
{
    private const string _openActionName = "Open";
    private const string _analyzeActionName = "Analyze";

    private const string _idActionParameterName = "Id";
    private const string _analysisKindParameterName = "AnalysisKind";

    private const string _reachingDefinitionsAnalysisKind = "ReachingDefinitions";
    private const string _callGraphAnalysisKind = "CallGraph";

    private readonly DotNetSolutionContext _solutionContext;
    private readonly DotNetTypes _dotNetTypes;
    private readonly ICodeNavigator? _codeNavigator;
    private readonly ISlice _slice;

    public DotNetMethodBrowser(DotNetSolutionContext solutionContext, DotNetTypes dotNetTypes, ICodeNavigator? codeNavigator = null)
    {
        _solutionContext = solutionContext;
        _dotNetTypes = dotNetTypes;
        _codeNavigator = codeNavigator;

        _slice = solutionContext.Slice;
    }

    public async Task<IModel> InitAsync()
    {
        return await DisplayMethodListAsync();
    }

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        IModel? result = null;

        if (command.Name == _openActionName && 
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            result = DisplayFlowGraph(new ElementId(id));
        }
        else if (command.Name == _analyzeActionName &&
            command.Parameters.TryGetValue(_idActionParameterName, out var id2) &&
            command.Parameters.TryGetValue(_analysisKindParameterName, out var analysisKind))
        {
            if (analysisKind == _reachingDefinitionsAnalysisKind)
            {
                result = DisplayReachingDefinitionsAnalysis(new ElementId(id2));
            }
            else if (analysisKind == _callGraphAnalysisKind)
            {
                result = await DisplayCallGraphAnalysisAsync(new ElementId(id2));
            }
        }

        return result;
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
            defUse.Definition,
            defUse.Use.Block,
            "is used in"));

        var graph = FlowGraphHelper.CreateGraphModel(flowGraph, additionalEdges);

        return graph;
    }

    private async Task<IModel> DisplayCallGraphAnalysisAsync(ElementId id)
    {
        var callGraph = await new CallGraph.Builder(_slice, _dotNetTypes)
            .AddCallerRoot(id)
            .BuildAsync();

        var explorer = new CallGraphExplorer(callGraph, _dotNetTypes, _solutionContext, _codeNavigator);
        return await explorer.InitAsync();
    }

    private async Task<IModel> DisplayMethodListAsync()
    {
        var items = new List<TreeItem>();

        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_slice, _dotNetTypes);
        
        foreach ((var method, var displayName) in methods)
        {
            items.Add(new TreeItem(
                displayName,
                [
                    new TreeItem("Control Flow Graph", [], CreateOpenCommand(method)),
                    new TreeItem("Reaching Definitions", [], CreateAnalyzeCommand(method, _reachingDefinitionsAnalysisKind)),
                    new TreeItem("Call Graph", [], CreateAnalyzeCommand(method, _callGraphAnalysisKind))
                ],
                null));
        }

        return new Tree([.. items]);
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
