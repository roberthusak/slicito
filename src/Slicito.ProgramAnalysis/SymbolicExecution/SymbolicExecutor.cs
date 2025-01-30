using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.Strings;
using Slicito.ProgramAnalysis.SymbolicExecution.Implementation;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

using VersionMap = System.Collections.Immutable.ImmutableDictionary<Slicito.ProgramAnalysis.SymbolicExecution.SmtLib.Function.Nullary, int>;

namespace Slicito.ProgramAnalysis.SymbolicExecution;

public sealed class SymbolicExecutor(ISolverFactory solverFactory)
{
    public async ValueTask<ExecutionResult> ExecuteAsync(
        IFlowGraph flowGraph,
        IEnumerable<BasicBlock> targetBlocks,
        IEnumerable<Expression>? initialConstraints = null)
    {
        return await new Executor(flowGraph, targetBlocks, initialConstraints ?? [], solverFactory).ExecuteAsync();
    }

    private class Executor
    {
        private readonly IFlowGraph _flowGraph;
        private readonly List<BasicBlock> _targetBlocks;
        private readonly List<Expression> _initialConstraints;
        private readonly ISolverFactory _solverFactory;

        private readonly Dictionary<BasicBlock, int> _topologicalOrder;
        private readonly SortedSet<ExecutionState> _workList;

        public Executor(IFlowGraph flowGraph, IEnumerable<BasicBlock> targetBlocks, IEnumerable<Expression> initialConstraints, ISolverFactory solverFactory)
        {
            _flowGraph = flowGraph;
            _targetBlocks = targetBlocks.ToList();
            _initialConstraints = initialConstraints.ToList();
            _solverFactory = solverFactory;

            var reachableBlocks = FlowGraphHelper.GetBlocksReachingTargets(_flowGraph, _targetBlocks);
            _topologicalOrder = FlowGraphHelper.GetBlockTopologicalOrder(_flowGraph, reachableBlocks);

            _workList = new SortedSet<ExecutionState>(new TopologicalOrderComparer(_topologicalOrder));
        }

        public async Task<ExecutionResult> ExecuteAsync()
        {
            if (!IsBlockConsidered(_flowGraph.Entry))
            {
                return new ExecutionResult.Unreachable();
            }

            var foundUnknown = false;

            Enqueue(CreateInitialState());

            while (TryDequeue(out var block, out var states))
            {
                var state = MergeStates(states);

                if (_targetBlocks.Contains(block))
                {
                    var result = await CheckReachabilityAsync(state);
                    switch (result)
                    {
                        case ExecutionResult.Reachable:
                            return result;
                        case ExecutionResult.Unreachable:
                            continue;
                        case ExecutionResult.Unknown:
                            foundUnknown = true;
                            break;
                    }
                }
    
                var (versionMap, conditionOperation, conditionValue) = CreateBlockConditions(block, state.VersionMap);

                Debug.Assert(state.UnmergedCondition is null);

                if (_flowGraph.GetUnconditionalSuccessor(block) is { } unconditionalSuccessor)
                {
                    if (IsBlockConsidered(unconditionalSuccessor))
                    {
                        var nextState = state with
                        {
                            CurrentBlock = unconditionalSuccessor,
                            VersionMap = versionMap,
                            UnmergedCondition = new(block, conditionOperation, versionMap),
                        };

                        Enqueue(nextState);
                    }
                }
                else
                {
                    if (_flowGraph.GetTrueSuccessor(block) is { } trueSuccessor && IsBlockConsidered(trueSuccessor))
                    {
                        var condition = Terms.And(conditionOperation, conditionValue);

                        var nextState = state with
                        {
                            CurrentBlock = trueSuccessor,
                            VersionMap = versionMap,
                            UnmergedCondition = new(block, condition, versionMap),
                        };

                        Enqueue(nextState);
                    }

                    if (_flowGraph.GetFalseSuccessor(block) is { } falseSuccessor && IsBlockConsidered(falseSuccessor))
                    {
                        var condition = Terms.And(conditionOperation, Terms.Not(conditionValue));

                        var nextState = state with
                        {
                            CurrentBlock = falseSuccessor,
                            VersionMap = versionMap,
                            UnmergedCondition = new(block, condition, versionMap),
                        };

                        Enqueue(nextState);
                    }
                }
            }

            return foundUnknown ? new ExecutionResult.Unknown() : new ExecutionResult.Unreachable();
        }

        private bool IsBlockConsidered(BasicBlock block) => _topologicalOrder.ContainsKey(block);

        private ExecutionState CreateInitialState()
        {
            // Create initial version map with parameter variables
            var versionMap = VersionMap.Empty;
            foreach (var param in _flowGraph.Entry.Parameters)
            {
                var paramFunc = new Function.Nullary(param.Name, GetSortForType(param.Type));
                versionMap = versionMap.Add(paramFunc, 0);
            }

            var condition = _initialConstraints.Aggregate((Term)Terms.True, (acc, constraint) => Terms.And(acc, TranslateExpression(constraint, versionMap)));

            return new ExecutionState(
                CurrentBlock: _flowGraph.Entry,
                VersionMap: versionMap,
                ConditionStack: new ImmutableConditionStack(condition),
                UnmergedCondition: null);
        }

        private void Enqueue(ExecutionState state) => _workList.Add(state);

        private bool TryDequeue([NotNullWhen(true)] out BasicBlock? block, [NotNullWhen(true)] out List<ExecutionState>? states)
        {
            if (_workList.Count == 0)
            {
                block = null;
                states = null;
                return false;
            }

            var first = _workList.First();
            block = first.CurrentBlock;

            states = [];
            while (first != null && first.CurrentBlock == block)
            {
                states.Add(first);
                _workList.Remove(first);

                first = _workList.FirstOrDefault();
            }

            return true;
        }

        private ExecutionState MergeStates(List<ExecutionState> states)
        {
            if (states.Count == 0)
            {
                throw new ArgumentException("Cannot merge empty list of states", nameof(states));
            }

            // All states should be at the same block and have unmerged conditions
            var block = states[0].CurrentBlock;
            Debug.Assert(states.All(s => s.CurrentBlock == block));

            // Merge version maps - take highest version for each variable
            var mergedVersionMap = states[0].VersionMap;
            foreach (var state in states.Skip(1))
            {
                foreach (var kvp in state.VersionMap)
                {
                    var func = kvp.Key;
                    var version = kvp.Value;
                    if (!mergedVersionMap.TryGetValue(func, out var currentVersion) || version > currentVersion)
                    {
                        mergedVersionMap = mergedVersionMap.SetItem(func, version);
                    }
                }
            }

            var mergedConditionStack = ImmutableConditionStack.Merge(
                states.Select(s => s.ConditionStack).ToList());

            var unmergedConditions = states
                .Select(s => s.UnmergedCondition)
                .OfType<UnmergedCondition>()        // Filter out nulls
                .ToList();

            if (unmergedConditions.Count > 0)
            {
                var disjuncts = unmergedConditions.Select(unmergedCondition =>
                {
                    var condition = (Term) CreateBlockReachingBoolean(unmergedCondition.PreviousBlock);
    
                    if (unmergedCondition.Condition != Terms.True)
                    {
                        condition = Terms.And(condition, unmergedCondition.Condition);
                    }
    
                    foreach (var kvp in unmergedCondition.VersionMap)
                    {
                        var func = kvp.Key;
                        var version = kvp.Value;
    
                        if (mergedVersionMap.TryGetValue(func, out var mergedVersion) && mergedVersion != version)
                        {
                            var stateVar = CreateVersionedConstant(func, version);
                            var mergedVar = CreateVersionedConstant(func, mergedVersion);
    
                            condition = Terms.And(condition, Terms.Equal(stateVar, mergedVar));
                        }
                    }
    
                    return condition;
                });
    
                var mergingCondition = Terms.Implies(CreateBlockReachingBoolean(block), disjuncts.Aggregate(Terms.Or));
    
                mergedConditionStack = mergedConditionStack.Push(mergingCondition);
            }

            return new ExecutionState(
                CurrentBlock: block,
                VersionMap: mergedVersionMap,
                ConditionStack: mergedConditionStack,
                UnmergedCondition: null);
        }

        private Term.FunctionApplication CreateBlockReachingBoolean(BasicBlock block)
        {
            var number = _topologicalOrder[block];
            var boolFunc = new Function.Nullary($"!reaching!{number}", Sorts.Bool);
            return Terms.Constant(boolFunc);
        }

        private async Task<ExecutionResult> CheckReachabilityAsync(ExecutionState state)
        {
            using var solver = await _solverFactory.CreateSolverAsync();

            foreach (var constraint in state.GetConditions())
            {
                await solver.AssertAsync(constraint);
            }

            await solver.AssertAsync(CreateBlockReachingBoolean(state.CurrentBlock));
            
            ExecutionModel? executionModel = null;
            var satResult = await solver.CheckSatisfiabilityAsync(async model =>
            {
                // If satisfiable, construct execution model from parameter values
                var parameterValues = new List<Expression.Constant>();
                foreach (var param in _flowGraph.Entry.Parameters)
                {
                    var paramFunc = new Function.Nullary($"{param.Name}", GetSortForType(param.Type));
                    var paramTerm = CreateVersionedConstant(paramFunc, 0);
                    var value = await model.EvaluateAsync(paramTerm);

                    parameterValues.Add(ConvertTermToConstant(value, param.Type));
                }

                executionModel = new ExecutionModel([.. parameterValues]);
            });

            return satResult switch
            {
                SolverResult.Satisfiable => new ExecutionResult.Reachable(executionModel ?? 
                    throw new InvalidOperationException("Model should be available for SAT result")),
                SolverResult.Unsatisfiable => new ExecutionResult.Unreachable(),
                SolverResult.Unknown => new ExecutionResult.Unknown()
            };
        }

        private (VersionMap versionMap, Term conditionOperation, Term conditionValue) CreateBlockConditions(BasicBlock block, VersionMap versionMap)
        {
            if (block is not BasicBlock.Inner inner || inner.Operation is null)
            {
                return (versionMap, Terms.True, Terms.True);
            }

            switch (inner.Operation)
            {
                case Operation.ConditionalJump jump:
                    return (versionMap, Terms.True, TranslateExpression(jump.Condition, versionMap));

                case Operation.Assignment assignment:
                    var (newVersionMap, assignmentTerm) = TranslateAssignment(assignment, versionMap);
                    return (newVersionMap, assignmentTerm, Terms.True);

                default:
                    throw new ArgumentException($"Unsupported operation type: {inner.Operation.GetType()}", nameof(block));
            }
        }

        private (VersionMap versionMap, Term term) TranslateAssignment(Operation.Assignment assignment, VersionMap versionMap)
        {
            if (assignment.Location is not Location.VariableReference varRef)
            {
                throw new ArgumentException("Only variable assignments are supported", nameof(assignment));
            }

            // Create new version for the assigned variable
            var varFunc = new Function.Nullary(varRef.Variable.Name, GetSortForType(varRef.Variable.Type));
            var newVersion = versionMap.TryGetValue(varFunc, out var currentVersion) ? currentVersion + 1 : 0;
            var newVersionMap = versionMap.SetItem(varFunc, newVersion);

            // Create equality between new version and translated expression
            var term = Terms.Equal(
                TranslateVariableReference(varRef.Variable, newVersionMap), 
                TranslateExpression(assignment.Value, versionMap));

            return (newVersionMap, term);
        }

        private Term TranslateExpression(Expression expr, VersionMap versionMap)
        {
            return expr switch
            {
                Expression.Constant.Boolean b => b.Value ? Terms.True : Terms.False,
                
                Expression.Constant.SignedInteger i => 
                    Terms.BitVec.Literal(ReinterpretAsUnsigned(i.Value), Sorts.BitVec(i.Type.Bits)),
                
                Expression.Constant.Utf16String s => Terms.String.Literal(s.Value),
                
                Expression.Constant.StringPattern p => TranslateStringPattern(p.Pattern),
                
                Expression.VariableReference varRef => TranslateVariableReference(varRef.Variable, versionMap),
                
                Expression.UnaryOperator op => TranslateUnaryOperator(op, versionMap),
                
                Expression.BinaryOperator op => TranslateBinaryOperator(op, versionMap),

                _ => throw new ArgumentException($"Unsupported expression type: {expr.GetType()}", nameof(expr))
            };
        }

        private Term TranslateStringPattern(StringPattern pattern)
        {
            return pattern switch
            {
                StringPattern.All => Terms.RegLan.All,

                StringPattern.Literal s =>
                    Terms.String.ToRegLan(
                        Terms.String.Literal(s.Value)),

                StringPattern.Character c => TranslateCharacterClass(c.CharacterClass),

                StringPattern.Concatenation c =>
                    Terms.RegLan.Concatenate(
                        TranslateStringPattern(c.Left),
                        TranslateStringPattern(c.Right)),
                
                StringPattern.Alternation a => Terms.RegLan.Union(
                    TranslateStringPattern(a.Left),
                    TranslateStringPattern(a.Right)),

                StringPattern.Loop { Pattern: var starPattern, Min: 0, Max: null } =>
                    Terms.RegLan.KleeneStar(TranslateStringPattern(starPattern)),

                StringPattern.Loop { Pattern: var plusPattern, Min: 1, Max: null } =>
                    Terms.RegLan.KleenePlus(TranslateStringPattern(plusPattern)),

                StringPattern.Loop { Pattern: var loopPattern, Min: var min, Max: int max } =>
                    Terms.RegLan.Loop(TranslateStringPattern(loopPattern), min, max),

                _ => throw new ArgumentException($"Unsupported string pattern: '{pattern.GetType().Name}'", nameof(pattern))
            };
        }

        private Term.FunctionApplication TranslateCharacterClass(CharacterClass characterClass)
        {
            return characterClass switch
            {
                CharacterClass.Any => Terms.RegLan.AllCharacters,

                CharacterClass.Single c =>
                    Terms.String.ToRegLan(
                        Terms.String.Literal(c.Value.ToString())),

                CharacterClass.Range range => Terms.RegLan.Range(
                    Terms.String.Literal(range.From.ToString()),
                    Terms.String.Literal(range.To.ToString())),

                CharacterClass.Union u => Terms.RegLan.Union(
                    TranslateCharacterClass(u.Left),
                    TranslateCharacterClass(u.Right)),

                _ => throw new ArgumentException($"Unsupported character class: '{characterClass.GetType().Name}'", nameof(characterClass))
            };
        }

        private Term TranslateVariableReference(Variable var, VersionMap versionMap)
        {
            var varFunc = new Function.Nullary(var.Name, GetSortForType(var.Type));
            var version = versionMap.TryGetValue(varFunc, out var v) ? v : 0;
            return CreateVersionedConstant(varFunc, version);
        }

        private static Term.FunctionApplication CreateVersionedConstant(Function.Nullary varFunc, int version)
        {
            return Terms.Constant(varFunc with { Name = $"{varFunc.Name}!{version}" });
        }

        private Term TranslateUnaryOperator(Expression.UnaryOperator op, VersionMap versionMap)
        {
            var operand = TranslateExpression(op.Operand, versionMap);

            return op.Kind switch
            {
                UnaryOperatorKind.Negate => Terms.BitVec.Negate(operand),
                UnaryOperatorKind.Not => Terms.Not(operand),
                UnaryOperatorKind.StringLength => Terms.Int.ToBitVec(
                    Terms.String.Length(operand),
                    ((DataType.Integer)op.GetDataType()).Bits),
                _ => throw new ArgumentException($"Unsupported unary operator: {op.Kind}", nameof(op))
            };
        }

        private Term TranslateBinaryOperator(Expression.BinaryOperator op, VersionMap versionMap)
        {
            var left = TranslateExpression(op.Left, versionMap);
            var right = TranslateExpression(op.Right, versionMap);

            return op.Kind switch
            {
                BinaryOperatorKind.Add => Terms.BitVec.Add(left, right),
                BinaryOperatorKind.Subtract => Terms.BitVec.Subtract(left, right),
                BinaryOperatorKind.Multiply => Terms.BitVec.Multiply(left, right),
                BinaryOperatorKind.And => Terms.And(left, right),
                BinaryOperatorKind.Or => Terms.Or(left, right),
                BinaryOperatorKind.Xor => Terms.Xor(left, right),
                BinaryOperatorKind.Equal => Terms.Equal(left, right),
                BinaryOperatorKind.NotEqual => Terms.Distinct(left, right),
                BinaryOperatorKind.LessThan => Terms.BitVec.SignedLessThan(left, right),
                BinaryOperatorKind.LessThanOrEqual => Terms.BitVec.SignedLessOrEqual(left, right),
                BinaryOperatorKind.GreaterThan => Terms.BitVec.SignedGreaterThan(left, right),
                BinaryOperatorKind.GreaterThanOrEqual => Terms.BitVec.SignedGreaterOrEqual(left, right),
                BinaryOperatorKind.StringStartsWith => Terms.String.IsPrefixOf(right, left),
                BinaryOperatorKind.StringEndsWith => Terms.String.IsSuffixOf(right, left),
                BinaryOperatorKind.StringMatchesPattern => Terms.String.IsInRegLan(left, right),
                _ => throw new ArgumentException($"Unsupported binary operator: {op.Kind}", nameof(op))
            };
        }

        private static Sort GetSortForType(DataType type) => type switch
        {
            DataType.Boolean => Sorts.Bool,
            DataType.Integer { Bits: var bits } => Sorts.BitVec(bits),
            DataType.Utf16String => Sorts.String,
            _ => throw new ArgumentException($"Unsupported type: {type}")
        };

        private static Expression.Constant ConvertTermToConstant(Term term, DataType type) => term switch
        {
            Term.Constant.Bool b => new Expression.Constant.Boolean(b.Value),
            Term.Constant.BitVec bv when type is DataType.Integer intType => 
                intType.Signed 
                    ? new Expression.Constant.SignedInteger(ReinterpretAsSigned(bv.Value), intType)
                    : new Expression.Constant.UnsignedInteger(bv.Value, intType),
            Term.Constant.String s => new Expression.Constant.Utf16String(s.Value),
            _ => throw new ArgumentException($"Unsupported term type: {term.GetType()}")
        };

        private static long ReinterpretAsSigned(ulong value) => (long)value;

        private static ulong ReinterpretAsUnsigned(long value) => (ulong)value;
    }
}
