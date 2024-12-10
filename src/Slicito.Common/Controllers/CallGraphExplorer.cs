
using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.Common.Controllers;

public class CallGraphExplorer(CallGraph callGraph, IProgramTypes types) : IController
{
    private const string _expandActionName = "Expand";

    private const string _idActionParameterName = "Id";

    private readonly HashSet<CallGraph.Procedure> _visibleProcedures = new(callGraph.RootProcedures);

    public async Task<IModel> InitAsync() => await CreateGraphAsync();

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _expandActionName &&
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            var elementId = new ElementId(id);
            var caller = callGraph.AllProcedures.Single(p => p.ProcedureElement.Id == elementId);

            _visibleProcedures.UnionWith(caller.CallSites.Select(callGraph.GetTarget));

            return await CreateGraphAsync();
        }

        return null;
    }

    private async Task<Graph> CreateGraphAsync()
    {
        var nameProvider = types.HasName(types.Procedure)
            ? callGraph.OriginalSlice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.Name)
            : null;

        var nodes = new List<Node>();
        var edges = new List<Edge>();

        foreach (var caller in _visibleProcedures)
        {
            var name = nameProvider is null
                ? ""
                : await nameProvider(caller.ProcedureElement.Id);

            var callerId = caller.ProcedureElement.Id.Value;

            bool expanded = true;

            foreach (var callSite in caller.CallSites)
            {
                var callee = callGraph.GetTarget(callSite);

                if (!_visibleProcedures.Contains(callee))
                {
                    expanded = false;
                    continue;
                }

                var edge = new Edge(caller.ProcedureElement.Id.Value, callee.ProcedureElement.Id.Value);
                edges.Add(edge);
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
}
