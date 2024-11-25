namespace Slicito.ProgramAnalysis.Notation;

public abstract class Expression
{
    private Expression() { }

    public abstract class Constant : Expression
    {
        private Constant() { }

        public sealed class SignedInteger(long value, DataType.Integer type) : Constant
        {
            public long Value { get; } = value;
            public DataType.Integer Type { get; } = type;
        }

        public sealed class UnsignedInteger(ulong value, DataType.Integer type) : Constant
        {
            public ulong Value { get; } = value;
            public DataType.Integer Type { get; } = type;
        }
    }

    public sealed class VariableReference(Variable variable) : Expression
    {
        public Variable Variable { get; } = variable;
    }

    public sealed class BinaryOperator(Expression left, Expression right, BinaryOperatorKind kind) : Expression
    {
        public Expression Left { get; } = left;
        public Expression Right { get; } = right;
        public BinaryOperatorKind Kind { get; } = kind;
    }
}
