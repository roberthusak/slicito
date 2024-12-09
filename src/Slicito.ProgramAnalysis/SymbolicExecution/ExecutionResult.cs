namespace Slicito.ProgramAnalysis.SymbolicExecution;

public abstract class ExecutionResult
{
    private ExecutionResult() { }

    public sealed class Reachable(ExecutionModel executionModel) : ExecutionResult
    {
        public ExecutionModel ExecutionModel { get; } = executionModel;
    }

    public sealed class Unreachable() : ExecutionResult;

    public sealed class Unknown() : ExecutionResult;
}
