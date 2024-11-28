using System.Collections.Immutable;

using Slicito.ProgramAnalysis.DataFlow.GenKill;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow.Analyses;

public static class ReachingDefinitions
{
    public readonly record struct Definition(BasicBlock Block, Operation Operation);

    public readonly record struct Use(BasicBlock Block, Operation Operation, Expression Expression);

    public record DefUse(Variable Variable, Definition Definition, Use Use);

    public static IDataFlowAnalysis<ImmutableHashSet<Operation>> Create() => GenKillSetDataFlowAnalysis.Create(new GenKillAnalysis());

    public static IEnumerable<DefUse> GetDefUses(AnalysisResult<ImmutableHashSet<Operation>> result)
    {
        var operationToBlockMap = result.InputMap.Keys
            .OfType<BasicBlock.Inner>()
            .Where(b => b.Operation != null)
            .ToDictionary(b => b.Operation!, b => b);

        foreach (var block in result.InputMap.Keys)
        {
            if (block is not BasicBlock.Inner inner || inner.Operation is null)
            {
                continue;
            }

            var variableReferences = GetVariableReferences(inner.Operation);
            
            foreach (var varRef in variableReferences)
            {
                var reachingDefs = result.InputMap[block];

                var relevantDefs = reachingDefs.OfType<Operation.Assignment>()
                    .Where(assignment => assignment.Location is Location.VariableReference locRef 
                        && locRef.Variable.Equals(varRef.Variable));

                foreach (var def in relevantDefs)
                {
                    var defBlock = operationToBlockMap[def];
                    yield return new DefUse(
                        varRef.Variable,
                        new Definition(defBlock, def),
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

    private class GenKillAnalysis : IGenKillAnalysis<Operation>
    {
        private Dictionary<Variable, ImmutableHashSet<Operation>>? _variableDefinitions;

        public AnalysisDirection Direction => AnalysisDirection.Forward;

        public GenKillMeetVariant MeetVariant => GenKillMeetVariant.Union;

        public void Initialize(IFlowGraph graph)
        {
            var variableDefinitionsBuilders = new Dictionary<Variable, HashSet<Operation>>();

            foreach (var block in graph.Blocks)
            {
                if (block is not BasicBlock.Inner inner || inner.Operation is not Operation.Assignment assignment)
                {
                    continue;
                }

                if (assignment.Location is not Location.VariableReference varRef)
                {
                    continue;
                }

                if (!variableDefinitionsBuilders.TryGetValue(varRef.Variable, out var defs))
                {
                    defs = [];
                    variableDefinitionsBuilders.Add(varRef.Variable, defs);
                }

                defs.Add(assignment);
            }

            _variableDefinitions = variableDefinitionsBuilders
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToImmutableHashSet());
        }

        public ImmutableHashSet<Operation> GetGen(BasicBlock block)
        {
            if (_variableDefinitions is null)
            {
                throw new InvalidOperationException("Analysis not initialized");
            }

            if (block is not BasicBlock.Inner inner || 
                inner.Operation is not Operation.Assignment assignment)
            {
                return [];
            }

            return [assignment];
        }

        public ImmutableHashSet<Operation> GetKill(BasicBlock block)
        {
            if (_variableDefinitions is null)
            {
                throw new InvalidOperationException("Analysis not initialized");
            }

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
                ? defs.Remove(assignment)
                : [];
        }
    }
}
