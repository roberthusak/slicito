using System.Diagnostics;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.DataFlow;
using Slicito.ProgramAnalysis.DataFlow.Analyses;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.Interprocedural;

/// <remarks>
/// Not thread safe, do not call its methods concurrently.
/// </remarks>
public class CallGraphBackwardDataFlowAnalyzer(CallGraph callGraph, IFlowGraphProvider flowGraphProvider)
{
    public record ValueSource(CallGraph.Procedure Procedure, BasicBlock Block, Variable Variable);

    private record WorkItem(CallGraph.Procedure Procedure, IFlowGraph FlowGraph, ReachingDefinitions.Use Use);

    private readonly CallGraph _callGraph = callGraph;
    private readonly IFlowGraphProvider _flowGraphProvider = flowGraphProvider;

    private readonly Dictionary<CallGraph.Procedure, List<ReachingDefinitions.DefUse>> _defUsesCache = [];

    public IEnumerable<ValueSource> FindValueSources(
        CallGraph.Procedure targetProcedure,
        Operation targetOperation,
        Expression targetExpression)
    {
        var targetFlowGraph = _flowGraphProvider.TryGetFlowGraph(targetProcedure.ProcedureElement)
            ?? throw new ArgumentException("Target procedure cannot be analyzed.", nameof(targetProcedure));

        if (targetExpression is not Expression.VariableReference { Variable: var variable })
        {
            throw new NotSupportedException("Other expressions than variable references are not supported.");
        }

        var block = targetFlowGraph.Blocks.Single(b => b is BasicBlock.Inner inner && inner.Operation == targetOperation);

        var workList = new Queue<WorkItem>([new (targetProcedure, targetFlowGraph, new (block, targetExpression))]);
        var processed = new HashSet<WorkItem>();

        while (workList.Count > 0)
        {
            var workItem = workList.Dequeue();
            if (!processed.Add(workItem))
            {
                continue;
            }

            var defUses = GetDefUses(workItem.Procedure, workItem.FlowGraph);
            foreach (var defUse in defUses)
            {
                if (defUse.Use != workItem.Use)
                {
                    continue;
                }

                switch (defUse.Definition)
                {
                    case BasicBlock.Inner { Operation: Operation.Assignment assignment }:
                        Debug.Assert(assignment.Location is Location.VariableReference varRef && varRef.Variable == defUse.Variable);
                        if (assignment.Value is Expression.VariableReference { Variable: var valueVariable })
                        {
                            // var a = b;
                            workList.Enqueue(
                                new (
                                    workItem.Procedure,
                                    workItem.FlowGraph,
                                    new (defUse.Definition, assignment.Value)));
                        }
                        else
                        {
                            // var a = ...; (complex expression - report as value source)
                            yield return new ValueSource(workItem.Procedure, defUse.Definition, defUse.Variable);
                        }
                        break;

                    case BasicBlock.Inner { Operation: Operation.Call call }:
                        var returnLocation = call.ReturnLocations.Single(l =>
                            l is Location.VariableReference { Variable: var returnVariable } && returnVariable == defUse.Variable);
                        var returnLocationIndex = call.ReturnLocations.IndexOf(returnLocation);
                        Debug.Assert(returnLocationIndex != -1);
                    
                        var calleeId = new ElementId(call.Signature.Name);
                        var calleeProcedure = _callGraph.AllProcedures.SingleOrDefault(p => p.ProcedureElement.Id == calleeId);
                        var calleeFlowGraph = _flowGraphProvider.TryGetFlowGraph(calleeId);

                        if (calleeFlowGraph is null || calleeProcedure is null)
                        {
                            // Procedure outside analysis or call graph
                            yield return new ValueSource(workItem.Procedure, defUse.Definition, defUse.Variable);
                        }
                        else
                        {
                            // Proceed to callee's return expression
                            workList.Enqueue(
                                new (
                                    calleeProcedure,
                                    calleeFlowGraph,
                                    new (
                                        calleeFlowGraph.Exit,
                                        calleeFlowGraph.Exit.ReturnValues[returnLocationIndex])));
                        }
                        break;

                    case BasicBlock.Entry entryBlock:
                        throw new NotSupportedException("Crossing entry block boundary is not supported yet.");

                    default:
                        throw new NotSupportedException($"Unsupported basic block or operation: {defUse.Definition}");
                }
            }
        }
    }

    private List<ReachingDefinitions.DefUse> GetDefUses(CallGraph.Procedure procedure, IFlowGraph flowGraph)
    {
        if (!_defUsesCache.TryGetValue(procedure, out var defUses))
        {
            var reachingDefinitions = ReachingDefinitions.Create();
            var result = AnalysisExecutor.Execute(flowGraph, reachingDefinitions);
            defUses = [.. ReachingDefinitions.GetDefUses(result)];

            _defUsesCache[procedure] = defUses;
        }

        return defUses;
    }
}
