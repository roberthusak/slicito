using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.Notation.TypedExpressions;
using Slicito.ProgramAnalysis.SymbolicExecution;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.ProgramAnalysis.Reachability;

public class ReachabilityAnalysis
{
    private readonly IFlowGraph _flowGraph;
    private readonly ProcedureReachabilityOptions _procedureReachabilityOptions;
    private readonly ISolverFactory _solverFactory;

    private ReachabilityAnalysis(IFlowGraph flowGraph, ProcedureReachabilityOptions procedureReachabilityOptions, ISolverFactory solverFactory)
    {
        _flowGraph = flowGraph;
        _procedureReachabilityOptions = procedureReachabilityOptions;
        _solverFactory = solverFactory;
    }

    public async Task<ReachabilityResult> AnalyzeAsync()
    {
        var executor = new SymbolicExecutor(_solverFactory);
        var result = await executor.ExecuteAsync(_flowGraph, [_flowGraph.Exit], _procedureReachabilityOptions.Constraints);

        return result switch
        {
            ExecutionResult.Reachable reachable => new ReachabilityResult.Reachable(CreateAssignments(reachable.ExecutionModel)),
            ExecutionResult.Unreachable => new ReachabilityResult.Unreachable(),
            ExecutionResult.Unknown => new ReachabilityResult.Unknown(),
            _ => throw new InvalidOperationException($"Unexpected execution result: {result}"),
        };
    }

    private ImmutableDictionary<Variable, Expression.Constant> CreateAssignments(ExecutionModel executionModel)
    {
        return executionModel.ParameterValues
            .Select((value, index) => (value, index))
            .ToImmutableDictionary(pair => _flowGraph.Entry.Parameters[pair.index], pair => pair.value);
    }

    private class ProcedureReachabilityOptions(IFlowGraph flowGraph) : IProcedureReachabilityOptions
    {
        private readonly List<Expression> _constraints = [];

        public IEnumerable<Expression> Constraints => _constraints;

        public BooleanExpression GetBooleanParameter(string name)
        {
            var variable = GetParameterVariable(name);

            if (variable.Type is not DataType.Boolean)
            {
                throw new InvalidOperationException($"Parameter '{name}' is not a boolean variable.");
            }

            return new BooleanExpression(new Expression.VariableReference(variable));
        }

        public IntegerExpression GetIntegerParameter(string name)
        {
            var variable = GetParameterVariable(name);

            if (variable.Type is not DataType.Integer)
            {
                throw new InvalidOperationException($"Parameter '{name}' is not an integer variable.");
            }

            return new IntegerExpression(new Expression.VariableReference(variable));
        }

        public StringExpression GetStringParameter(string name)
        {
            var variable = GetParameterVariable(name);

            if (variable.Type is not DataType.Utf16String)
            {
                throw new InvalidOperationException($"Parameter '{name}' is not a string variable.");
            }

            return new StringExpression(new Expression.VariableReference(variable));
        }

        public BooleanExpression GetBooleanReturnValue()
        {
            var variable = GetReturnValueVariable();

            if (variable.Type is not DataType.Boolean)
            {
                throw new InvalidOperationException("Return value is not a boolean variable.");
            }

            return new BooleanExpression(new Expression.VariableReference(variable));
        }

        public IntegerExpression GetIntegerReturnValue()
        {
            var variable = GetReturnValueVariable();

            if (variable.Type is not DataType.Integer)
            {
                throw new InvalidOperationException("Return value is not an integer variable.");
            }

            return new IntegerExpression(new Expression.VariableReference(variable));
        }

        public StringExpression GetStringReturnValue()
        {
            var variable = GetReturnValueVariable();

            if (variable.Type is not DataType.Utf16String)
            {
                throw new InvalidOperationException("Return value is not a string variable.");
            }

            return new StringExpression(new Expression.VariableReference(variable));
        }

        private Variable GetParameterVariable(string name)
        {
            return flowGraph.Entry.Parameters.FirstOrDefault(v => v.Name == name)
                ?? throw new ArgumentException($"Parameter '{name}' not found in procedure.");
        }

        private Variable GetReturnValueVariable()
        {
            var returnValues = flowGraph.Exit.ReturnValues;
            if (returnValues.Length != 1)
            {
                throw new InvalidOperationException($"Procedure has {returnValues.Length} return values.");
            }

            if (returnValues[0] is not Expression.VariableReference { Variable: var variable })
            {
                throw new InvalidOperationException("Return value is not a variable reference.");
            }

            return variable;
        }

        public IProcedureReachabilityOptions AddConstraint(BooleanExpression constraint)
        {
            _constraints.Add(constraint.Expression);
            return this;
        }
    }

    public class Builder(IFlowGraphProvider flowGraphProvider, ISolverFactory solverFactory)
    {
        private IFlowGraph? _flowGraph;
        private ProcedureReachabilityOptions? _procedureReachabilityOptions;

        public Builder WithProcedureEntryToExit(ElementId procedure, Action<IProcedureReachabilityOptions>? configureOptions = null)
        {
            if (_flowGraph is not null)
            {
                throw new NotSupportedException("Only one procedure can be analyzed at a time.");
            }

            _flowGraph = flowGraphProvider.TryGetFlowGraph(procedure);
            if (_flowGraph is null)
            {
                throw new ArgumentException($"Flow graph for procedure '{procedure.Value}' not found.");
            }

            _procedureReachabilityOptions = new ProcedureReachabilityOptions(_flowGraph);
            configureOptions?.Invoke(_procedureReachabilityOptions);

            return this;
        }

        public ReachabilityAnalysis Build()
        {
            if (_flowGraph is null)
            {
                throw new InvalidOperationException("No procedure has been specified.");
            }

            return new ReachabilityAnalysis(_flowGraph, _procedureReachabilityOptions!, solverFactory);
        }
    }
}
