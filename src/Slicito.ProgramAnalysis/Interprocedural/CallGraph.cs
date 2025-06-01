using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.ProgramAnalysis.Interprocedural;

public sealed class CallGraph
{
    private readonly Dictionary<ElementId, Procedure> _proceduresById;
    private readonly Dictionary<(ElementId, int), ElementId> _callTargets; // Maps call site ID and ordinal to target procedure ID

    public record CallSite(ElementInfo ProcedureElement, ElementInfo CallElement, int Ordinal);
    public record Procedure(ElementInfo ProcedureElement, ImmutableArray<CallSite> CallSites);

    public ImmutableArray<Procedure> RootProcedures { get; }
    public ImmutableArray<Procedure> AllProcedures { get; }

    public ISlice OriginalSlice { get; }

    private CallGraph(
        ImmutableArray<Procedure> rootProcedures,
        ImmutableArray<Procedure> allProcedures,
        Dictionary<ElementId, Procedure> proceduresById,
        Dictionary<(ElementId, int), ElementId> callTargets,
        ISlice originalSlice)
    {
        RootProcedures = rootProcedures;
        AllProcedures = allProcedures;
        _proceduresById = proceduresById;
        _callTargets = callTargets;
        OriginalSlice = originalSlice;
    }

    public Procedure GetTarget(CallSite callSite)
    {
        var targetProcedureId = _callTargets[(callSite.CallElement.Id, callSite.Ordinal)];
        return _proceduresById[targetProcedureId];
    }

    public IEnumerable<CallSite> GetCallers(Procedure callee)
    {
        return AllProcedures
            .SelectMany(p => p.CallSites)
            .Where(cs => _callTargets[(cs.CallElement.Id, cs.Ordinal)] == callee.ProcedureElement.Id);
    }

    public class Builder(ISlice slice, IProgramTypes types)
    {
        private readonly HashSet<ElementId> _rootProcedureIds = [];

        public Builder AddCallerRoot(ElementId callerRoot)
        {
            _rootProcedureIds.Add(callerRoot);
            return this;
        }

        public async Task<CallGraph> BuildAsync()
        {
            var proceduresById = new Dictionary<ElementId, Procedure>();
            var callTargets = new Dictionary<(ElementId, int), ElementId>();
            var proceduresToProcess = new Queue<ElementId>(_rootProcedureIds);
            
            var containsExplorer = slice.GetLinkExplorer(types.Contains);
            var callsExplorer = slice.GetLinkExplorer(types.Calls);

            // Process procedures breadth-first to discover the complete call graph
            while (proceduresToProcess.Count > 0)
            {
                var procedureId = proceduresToProcess.Dequeue();
                
                // Skip if already processed
                if (proceduresById.ContainsKey(procedureId))
                {
                    continue;
                }

                var procedureElement = new ElementInfo(procedureId, types.Procedure);
                
                // Find all calls within this procedure and its nested functions (local functions, lambdas)
                
                var containedElements = (await containsExplorer.GetTargetElementsAsync(procedureId)).ToList();

                var operationElements = containedElements.Where(e => e.Type.Value.IsSubsetOfOrEquals(types.Operation.Value)).ToList();

                var nestedProcedureElements = containedElements.Where(e => e.Type.Value.IsSubsetOfOrEquals(types.NestedProcedures.Value));
                foreach (var nestedProcedureElement in nestedProcedureElements)
                {
                    var nestedContainedElements = await containsExplorer.GetTargetElementsAsync(nestedProcedureElement.Id);
                    var nestedOperationElements = nestedContainedElements.Where(e => e.Type.Value.IsSubsetOfOrEquals(types.Operation.Value));
                    operationElements.AddRange(nestedOperationElements);
                }

                var callSites = new List<CallSite>();

                foreach (var callElement in operationElements)
                {
                    var targetElements = await callsExplorer.GetTargetElementsAsync(callElement.Id);
                    
                    foreach (var (targetElement, ordinal) in targetElements.Select((e, i) => (e, i)))
                    {
                        callSites.Add(new(procedureElement, callElement, ordinal));
                        callTargets[(callElement.Id, ordinal)] = targetElement.Id;
                        proceduresToProcess.Enqueue(targetElement.Id);
                    }
                }

                var procedure = new Procedure(procedureElement, [.. callSites]);
                proceduresById[procedureId] = procedure;
            }

            var rootProcedures = _rootProcedureIds
                .Select(id => proceduresById[id])
                .ToImmutableArray();

            return new CallGraph(
                rootProcedures,
                proceduresById.Values.ToImmutableArray(),
                proceduresById,
                callTargets,
                slice);
        }
    }
}
