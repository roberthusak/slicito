using System.Collections.Immutable;
using Slicito.Abstractions;

namespace Slicito.ProgramAnalysis.Interprocedural;

public sealed class CallGraph
{
    private readonly Dictionary<ElementId, Procedure> _proceduresById;
    private readonly Dictionary<ElementId, ElementId> _callTargets; // Maps call site ID to target procedure ID

    public record CallSite(ElementInfo ProcedureElement, ElementInfo CallElement);
    public record Procedure(ElementInfo ProcedureElement, ImmutableArray<CallSite> CallSites);

    public ImmutableArray<Procedure> RootProcedures { get; }
    public ImmutableArray<Procedure> AllProcedures { get; }

    public ISlice OriginalSlice { get; }

    private CallGraph(
        ImmutableArray<Procedure> rootProcedures,
        ImmutableArray<Procedure> allProcedures,
        Dictionary<ElementId, Procedure> proceduresById,
        Dictionary<ElementId, ElementId> callTargets,
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
        var targetProcedureId = _callTargets[callSite.CallElement.Id];
        return _proceduresById[targetProcedureId];
    }

    public IEnumerable<CallSite> GetCallers(Procedure callee)
    {
        return AllProcedures
            .SelectMany(p => p.CallSites)
            .Where(cs => _callTargets[cs.CallElement.Id] == callee.ProcedureElement.Id);
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
            var callTargets = new Dictionary<ElementId, ElementId>();
            var proceduresToProcess = new Queue<ElementId>(_rootProcedureIds);
            
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
                
                // Find all calls within this procedure
                var containsExplorer = slice.GetLinkExplorer(types.Contains);
                var callElements = await containsExplorer.GetTargetElementsAsync(procedureId);
                var callSites = new List<CallSite>();

                foreach (var callElement in callElements.Where(e => e.Type == types.Call))
                {
                    var callSite = new CallSite(procedureElement, callElement);
                    callSites.Add(callSite);

                    // Find target procedure of this call
                    var callsExplorer = slice.GetLinkExplorer(types.Calls);
                    var targetElements = await callsExplorer.GetTargetElementsAsync(callElement.Id);
                    var targetElement = targetElements.Single();
                    
                    callTargets[callElement.Id] = targetElement.Id;
                    proceduresToProcess.Enqueue(targetElement.Id);
                }

                var procedure = new Procedure(procedureElement, callSites.ToImmutableArray());
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
