using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.DataFlow;
using Slicito.ProgramAnalysis.DataFlow.Analyses;
using Slicito.ProgramAnalysis.Interprocedural;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.Common.Controllers.Implementation;

public static class InterproceduralDataFlowAnalyzer
{
    public record ProcedureParameter(CallGraph.Procedure Procedure, Variable Parameter);

    public static HashSet<ProcedureParameter> FindReachableProcedureParameters(CallGraph callGraph, IFlowGraphProvider flowGraphProvider, IEnumerable<ProcedureParameter> initialParameters)
    {
        return new Analyzer(callGraph, flowGraphProvider, initialParameters).Analyze();
    }

    private class Analyzer(CallGraph callGraph, IFlowGraphProvider flowGraphProvider, IEnumerable<ProcedureParameter> initialParameters)
    {
        private readonly HashSet<ProcedureParameter> _reachableParameters = new(initialParameters);

        private readonly Queue<ProcedureParameter> _worklist = new(initialParameters);

        private readonly Dictionary<string, CallGraph.Procedure> _signatureNameToProcedureMap = callGraph.AllProcedures
            .ToDictionary(
                procedure => flowGraphProvider.GetProcedureSignature(procedure.ProcedureElement).Name,
                procedure => procedure);

        private readonly Dictionary<CallGraph.Procedure, List<ReachingDefinitions.DefUse>> _defUsesCache = [];

        public HashSet<ProcedureParameter> Analyze()
        {
            while (_worklist.Count > 0)
            {
                var parameter = _worklist.Dequeue();

                foreach (var reachableParameter in GetReachableParameters(parameter))
                {
                    if (_reachableParameters.Add(reachableParameter))
                    {
                        _worklist.Enqueue(reachableParameter);
                    }
                }
            }

            return _reachableParameters;
        }

        private IEnumerable<ProcedureParameter> GetReachableParameters(ProcedureParameter inputParameter)
        {
            var procedure = inputParameter.Procedure;
            var parameter = inputParameter.Parameter;

            var flowGraph = flowGraphProvider.TryGetFlowGraph(procedure.ProcedureElement);
            if (flowGraph is null)
            {
                yield break;
            }

            if (!_defUsesCache.TryGetValue(procedure, out var defUses))
            {
                var reachingDefinitions = ReachingDefinitions.Create();
                var result = AnalysisExecutor.Execute(flowGraph, reachingDefinitions);
                defUses = ReachingDefinitions.GetDefUses(result).ToList();

                _defUsesCache[procedure] = defUses;
            }

            var initialDefUses = defUses
                .Where(defUse => defUse.Definition == flowGraph.Entry && defUse.Variable == parameter);

            var defUseWorklist = new Queue<ReachingDefinitions.DefUse>(initialDefUses);

            var processedDefUses = new HashSet<ReachingDefinitions.DefUse>();

            while (defUseWorklist.Count > 0)
            {
                var defUse = defUseWorklist.Dequeue();

                if (!processedDefUses.Add(defUse))
                {
                    continue;
                }

                if (defUse.Use.Block is not BasicBlock.Inner inner)
                {
                    continue;
                }

                switch (inner.Operation)
                {
                    case Operation.Call call:
                        var targetProcedure = _signatureNameToProcedureMap[call.Signature.Name];
                        var calleeFlowGraph = flowGraphProvider.TryGetFlowGraph(targetProcedure.ProcedureElement);
                        if (calleeFlowGraph is not null)
                        {
                            for (var i = 0; i < call.Arguments.Length; i++)
                            {
                                var argument = call.Arguments[i];
                                var calleeParameter = calleeFlowGraph.Entry.Parameters[i];

                                if (argument.Contains(defUse.Use.Expression))
                                {
                                    yield return new ProcedureParameter(targetProcedure, calleeParameter);
                                }
                            }
                        }
                        break;

                    case Operation.Assignment assignment:
                        foreach (var followingDefUse in defUses.Where(du => du.Definition == defUse.Use.Block))
                        {
                            defUseWorklist.Enqueue(followingDefUse);
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
