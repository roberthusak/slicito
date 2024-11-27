namespace Slicito.ProgramAnalysis.Notation;

public abstract class Expression
{
    private Expression() { }

    public abstract class Constant : Expression
    {
        private Constant() { }

        public sealed class Boolean(bool value) : Constant
        {
            public bool Value { get; } = value;
        }

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

        public sealed class Float(double value, DataType.Float type) : Constant
        {
            public double Value { get; } = value;
            public DataType.Float Type { get; } = type;
        }
    }

    public sealed class VariableReference(Variable variable) : Expression
    {
        public Variable Variable { get; } = variable;
    }

    public sealed class BinaryOperator(BinaryOperatorKind kind, Expression left, Expression right) : Expression
    {
        public BinaryOperatorKind Kind { get; } = kind;
        public Expression Left { get; } = left;
        public Expression Right { get; } = right;
    }
}
