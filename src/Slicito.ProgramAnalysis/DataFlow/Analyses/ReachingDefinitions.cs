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
                    .Where(defBlock =>
                        (defBlock is BasicBlock.Inner inner && inner.Operation is Operation.Assignment assignment
                        && assignment.Location is Location.VariableReference locRef
                        && locRef.Variable.Equals(varRef.Variable))
                        || (defBlock is BasicBlock.Entry entry
                        && entry.Parameters.Contains(varRef.Variable)));

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

    private static IEnumerable<Expression.VariableReference> GetVariableReferences(Operation operation)
    {
        return operation switch
        {
            Operation.Assignment assignment => GetVariableReferences(assignment.Value),
            Operation.ConditionalJump jump => GetVariableReferences(jump.Condition),
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
                if (block is BasicBlock.Inner inner && inner.Operation is Operation.Assignment assignment)
                {
                    if (assignment.Location is not Location.VariableReference varRef)
                    {
                        continue;
                    }

                    var defs = GetOrCreateDefinitions(variableDefinitionsBuilders, varRef.Variable);
                    defs.Add(block);
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
                || (block is BasicBlock.Inner inner && inner.Operation is Operation.Assignment))
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

            // The state before the entry block is empty, no need to kill anything
            if (block is not BasicBlock.Inner inner || 
                inner.Operation is not Operation.Assignment assignment)
            {
                return [];
            }

            if (assignment.Location is not Location.VariableReference varRef)
            {
                return [];
            }

            return _variableDefinitions.TryGetValue(varRef.Variable, out var defs)
                ? defs.Remove(block)
                : [];
        }
    }
}
