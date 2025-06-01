
using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.Abstractions.Models;
using Slicito.Common.Controllers.Implementation;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.Common.Controllers;

public class CallGraphExplorer : IController
{
    public class Options
    {
        internal HashSet<string> EmphasizedParameterNames { get; } = [];

        public Options EmphasizeParameterDataFlow(string parameterName)
        {
            EmphasizedParameterNames.Add(parameterName);
            return this;
        }
    }

    private const string _expandActionName = "Expand";

    private const string _idActionParameterName = "Id";

    private readonly CallGraph _callGraph;
    private readonly IProgramTypes _types;
    private readonly ICodeNavigator? _codeNavigator;

    private readonly HashSet<CallGraph.Procedure> _visibleProcedures;

    public CallGraphExplorer(
        CallGraph callGraph,
        IProgramTypes types,
        IFlowGraphProvider flowGraphProvider,
        ICodeNavigator? codeNavigator = null,
        Action<Options>? configureOptions = null)
    {
        _callGraph = callGraph;
        _types = types;
        _codeNavigator = codeNavigator;

        var options = new Options();
        configureOptions?.Invoke(options);

        _visibleProcedures = CreateInitialVisibleProcedures(callGraph, flowGraphProvider, options);
    }

    private static HashSet<CallGraph.Procedure> CreateInitialVisibleProcedures(CallGraph callGraph, IFlowGraphProvider flowGraphProvider, Options options)
    {
        var visibleProcedures = new HashSet<CallGraph.Procedure>(callGraph.RootProcedures);

        var initialParameters = callGraph.RootProcedures
            .SelectMany(procedure =>
                (flowGraphProvider.TryGetFlowGraph(procedure.ProcedureElement)?.Entry.Parameters ?? [])
                .Where(parameter => options.EmphasizedParameterNames.Contains(parameter.Name))
                .Select(parameter =>
                    new InterproceduralDataFlowAnalyzer.ProcedureParameter(procedure, parameter)))
            .ToArray();

        if (initialParameters.Length > 0)
        {
            var reachableParameters = InterproceduralDataFlowAnalyzer.FindReachableProcedureParameters(callGraph, flowGraphProvider, initialParameters);
            visibleProcedures.UnionWith(reachableParameters.Select(p => p.Procedure));
        }

        return visibleProcedures;
    }

    public async Task<IModel> InitAsync() => await CreateGraphAsync();

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _expandActionName &&
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            var elementId = new ElementId(id);
            var caller = _callGraph.AllProcedures.Single(p => p.ProcedureElement.Id == elementId);

            _visibleProcedures.UnionWith(
                caller.CallSites
                .Select(_callGraph.GetTarget)
                .SelectMany(ct => ct.All));

            await TryNavigateToAsync(caller.ProcedureElement);

            return await CreateGraphAsync();
        }

        return null;
    }

    private async Task TryNavigateToAsync(ElementInfo procedureElement)
    {
        if (_codeNavigator is null || !_types.HasCodeLocation(_types.Procedure))
        {
            return;
        }

        var codeLocationProvider = _callGraph.OriginalSlice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.CodeLocation);
        var codeLocationString = await codeLocationProvider(procedureElement.Id);
        var codeLocation = CodeLocation.Parse(codeLocationString);

        if (codeLocation is null)
        {
            return;
        }

        await _codeNavigator.NavigateToAsync(codeLocation);
    }

    private async Task<Graph> CreateGraphAsync()
    {
        var nameProvider = _types.HasName(_types.Procedure)
            ? _callGraph.OriginalSlice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.Name)
            : null;

        var nodes = new List<Node>();
        var edges = new List<Edge>();

        foreach (var caller in _visibleProcedures)
        {
            var name = nameProvider is null
                ? ""
                : await nameProvider(caller.ProcedureElement.Id);

            var callerId = caller.ProcedureElement.Id.Value;

            var expanded = true;

            foreach (var callSite in caller.CallSites)
            {
                foreach (var callee in _callGraph.GetTarget(callSite).All)
                {
                    if (!_visibleProcedures.Contains(callee))
                    {
                        expanded = false;
                        break;
                    }

                    var edge = new Edge(caller.ProcedureElement.Id.Value, callee.ProcedureElement.Id.Value);
                    edges.Add(edge);
                }
            }

            if (!expanded)
            {
                name += " (+)";
            }

            var node = new Node(callerId, name, CreateExpandCommand(caller.ProcedureElement));
            nodes.Add(node);
        }

        return new Graph([.. nodes], [.. edges]);
    }

    private static Command CreateExpandCommand(ElementInfo element)
    {
        return new(
            _expandActionName,
            ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }

    public bool IsVisible(CallGraph.Procedure procedure) => _visibleProcedures.Contains(procedure);

    public bool IsExpanded(CallGraph.Procedure procedure) =>
        procedure.CallSites
            .Select(_callGraph.GetTarget)
            .SelectMany(ct => ct.All)
            .All(_visibleProcedures.Contains);
}
