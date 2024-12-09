using System.Collections.Immutable;

using Slicito.ProgramAnalysis.DataFlow.GenKill;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow.Analyses;

public static class ReachingDefinitions
{
    public readonly record struct Use(BasicBlock Block, Operation Operation, Expression Expression);

    public record DefUse(Variable Variable, BasicBlock Definition, Use Use);

    public static IDataFlowAnalysis<ImmutableHashSet<BasicBlock>> Create() => GenKillSetDataFlowAnalysis.Create(new GenKillAnalysis());

    public static IEnumerable<DefUse> GetDefUses(AnalysisResult<ImmutableHashSet<BasicBlock>> result)
    {
        foreach (var block in result.InputMap.Keys)
        {
            if (block is not BasicBlock.Inner inner || inner.Operation is null)
            {
                continue;
            }

            var variableReferences = GetVariableReferences(inner.Operation);
            
            foreach (var varRef in variableReferences)
            {
                var reachingDefBlocks = result.InputMap[block];

                var relevantDefs = reachingDefBlocks
                    .Where(defBlock => ContainsVariableDefinition(defBlock, varRef.Variable));

                foreach (var def in relevantDefs)
                {
                    yield return new DefUse(
                        varRef.Variable,
                        def,
                        new Use(block, inner.Operation, varRef));
                }
            }
        }
    }

    private static bool ContainsVariableDefinition(BasicBlock defBlock, Variable variable)
    {
        switch (defBlock)
        {
            case BasicBlock.Inner inner when inner.Operation is Operation.Assignment assignment:
                if (assignment.Location is Location.VariableReference locRef)
                {
                    return locRef.Variable.Equals(variable);
                }
                return false;

            case BasicBlock.Inner inner when inner.Operation is Operation.Call call:
                return call.ReturnLocations
                    .Select(loc => (loc as Location.VariableReference)?.Variable)
                    .Contains(variable);

            case BasicBlock.Entry entry:
                return entry.Parameters.Contains(variable);

            default:
                return false;
        }
    }

    private static IEnumerable<Expression.VariableReference> GetVariableReferences(Operation operation)
    {
        return operation switch
        {
            Operation.Assignment assignment => GetVariableReferences(assignment.Value),
            Operation.ConditionalJump jump => GetVariableReferences(jump.Condition),
            Operation.Call call => call.Arguments
                .SelectMany(GetVariableReferences),
            _ => []
        };
    }

    private static IEnumerable<Expression.VariableReference> GetVariableReferences(Expression expression)
    {
        return expression switch
        {
            Expression.VariableReference varRef => [varRef],
            Expression.BinaryOperator binary => GetVariableReferences(binary.Left)
                .Concat(GetVariableReferences(binary.Right)),
            _ => []
        };
    }

    private class GenKillAnalysis : IGenKillAnalysis<BasicBlock>
    {
        private Dictionary<Variable, ImmutableHashSet<BasicBlock>>? _variableDefinitions;

        public AnalysisDirection Direction => AnalysisDirection.Forward;

        public GenKillMeetVariant MeetVariant => GenKillMeetVariant.Union;

        public void Initialize(IFlowGraph graph)
        {
            var variableDefinitionsBuilders = new Dictionary<Variable, HashSet<BasicBlock>>();

            foreach (var block in graph.Blocks)
            {
                if (block is BasicBlock.Inner inner)
                {
                    if (inner.Operation is Operation.Assignment assignment)
                    {
                        if (assignment.Location is not Location.VariableReference varRef)
                        {
                            continue;
                        }

                        var defs = GetOrCreateDefinitions(variableDefinitionsBuilders, varRef.Variable);
                        defs.Add(block);
                    }
                    else if (inner.Operation is Operation.Call call)
                    {
                        if (call.ReturnLocations.Length > 1)
                        {
                            throw new NotSupportedException("Call nodes with multiple return locations not supported");
                        }

                        if (call.ReturnLocations.Length == 1 && call.ReturnLocations[0] is Location.VariableReference varRef)
                        {
                            var defs = GetOrCreateDefinitions(variableDefinitionsBuilders, varRef.Variable);
                            defs.Add(block);
                        }
                    }
                }
                else if (block is BasicBlock.Entry entry)
                {
                    foreach (var parameter in entry.Parameters)
                    {
                        var defs = GetOrCreateDefinitions(variableDefinitionsBuilders, parameter);
                        defs.Add(block);
                    }
                }
            }

            _variableDefinitions = variableDefinitionsBuilders
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToImmutableHashSet());

            static HashSet<BasicBlock> GetOrCreateDefinitions(Dictionary<Variable, HashSet<BasicBlock>> variableDefinitionsBuilders, Variable variable)
            {
                if (!variableDefinitionsBuilders.TryGetValue(variable, out var defs))
                {
                    defs = [];
                    variableDefinitionsBuilders.Add(variable, defs);
                }

                return defs;
            }
        }

        public ImmutableHashSet<BasicBlock> GetGen(BasicBlock block)
        {
            if (_variableDefinitions is null)
            {
                throw new InvalidOperationException("Analysis not initialized");
            }

            if (block is BasicBlock.Entry
                || (block is BasicBlock.Inner inner
                    && (inner.Operation is Operation.Assignment
                        || inner.Operation is Operation.Call)))
            {
                return [block];
            }

            return [];
        }

        public ImmutableHashSet<BasicBlock> GetKill(BasicBlock block)
        {
            if (_variableDefinitions is null)
            {
                throw new InvalidOperationException("Analysis not initialized");
            }

            var killBlocks = ImmutableHashSet.CreateBuilder<BasicBlock>();

            foreach (var kvp in _variableDefinitions)
            {
                var defs = kvp.Value;
                if (defs.Contains(block))
                {
                    killBlocks.UnionWith(defs.Where(def => def != block));
                }
            }

            return killBlocks.ToImmutableHashSet();
        }
    }
}
