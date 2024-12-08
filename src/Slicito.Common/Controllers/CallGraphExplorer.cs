
using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.Common.Controllers;

public class CallGraphExplorer(CallGraph callGraph, IProgramTypes types) : IController
{
    public async Task<IModel> InitAsync() => await CreateGraphAsync();

    public async Task<IModel?> ProcessCommandAsync(Command command) => await CreateGraphAsync();

    private async Task<Graph> CreateGraphAsync()
    {
        var nameProvider = types.HasName(types.Procedure)
            ? callGraph.OriginalSlice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.Name)
            : null;

        var nodes = new List<Node>();
        var edges = new List<Edge>();

        foreach (var procedure in callGraph.AllProcedures)
        {
            var name = nameProvider is null
                ? null
                : await nameProvider(procedure.ProcedureElement.Id);

            var node = new Node(procedure.ProcedureElement.Id.Value, name);
            nodes.Add(node);

            foreach (var callSite in procedure.CallSites)
            {
                var callee = callGraph.GetTarget(callSite);

                var edge = new Edge(procedure.ProcedureElement.Id.Value, callee.ProcedureElement.Id.Value);
                edges.Add(edge);
            }
        }

        return new Graph([.. nodes], [.. edges]);
    }
}
