namespace Slicito.ProgramAnalysis.Notation;

public abstract class Operation
{
    private Operation() { }

    public sealed class ConditionalJump(Expression condition) : Operation
    {
        public Expression Condition { get; } = condition;
    }

    public sealed class Assignment(Location location, Expression value) : Operation
    {
        public Location Location { get; } = location;
        public Expression Value { get; } = value;
    }
}
