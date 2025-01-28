namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

// The equality operator overloads are for constructing new expressions, so neiter Equals nor GetHashCode are needed.
#pragma warning disable CS0660
#pragma warning disable CS0661

public readonly struct IntegerExpression
{
    public Expression Expression { get; }

    public DataType.Integer Type { get; }

    public IntegerExpression(Expression expression)
    {
        Expression = expression;

        Type = expression.GetDataType() as DataType.Integer
            ?? throw new InvalidOperationException("The expression is not an integer expression.");
    }

    public static implicit operator IntegerExpression(int value)
    {
        return new IntegerExpression(new Expression.Constant.SignedInteger(value, new DataType.Integer(Signed: false, Bits: 32)));
    }

    private IntegerExpression(Expression expression, DataType.Integer type)
    {
        Expression = expression;
        Type = type;
    }

    public static IntegerExpression operator +(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.Add, left, right);

    public static IntegerExpression operator -(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.Subtract, left, right);

    public static IntegerExpression operator *(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.Multiply, left, right);

    public static IntegerExpression operator /(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.Divide, left, right);

    public static IntegerExpression operator %(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.Modulo, left, right);

    public static IntegerExpression operator <<(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.ShiftLeft, left, right);

    public static IntegerExpression operator >>(IntegerExpression left, IntegerExpression right) =>
        CreateArithmeticOperatorExpression(BinaryOperatorKind.ShiftRight, left, right);

    public static BooleanExpression operator ==(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.Equal, left, right);

    public static BooleanExpression operator !=(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.NotEqual, left, right);

    public static BooleanExpression operator <(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.LessThan, left, right);

    public static BooleanExpression operator <=(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.LessThanOrEqual, left, right);

    public static BooleanExpression operator >(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.GreaterThan, left, right);

    public static BooleanExpression operator >=(IntegerExpression left, IntegerExpression right) =>
        CreateRelationalOperatorExpression(BinaryOperatorKind.GreaterThanOrEqual, left, right);

    private static IntegerExpression CreateArithmeticOperatorExpression(BinaryOperatorKind kind, IntegerExpression left, IntegerExpression right)
    {
        CheckTypesAreSame(left.Type, right.Type);

        return new IntegerExpression(
            new Expression.BinaryOperator(kind, left.Expression, right.Expression),
            left.Type
        );
    }

    private static BooleanExpression CreateRelationalOperatorExpression(BinaryOperatorKind kind, IntegerExpression left, IntegerExpression right)
    {
        return new BooleanExpression(
            new Expression.BinaryOperator(
                kind,
                left.Expression,
                right.Expression
            )
        );
    }

    private static void CheckTypesAreSame(DataType.Integer left, DataType.Integer right)
    {
        if (left.Signed != right.Signed || left.Bits != right.Bits)
        {
            throw new InvalidOperationException("Types are not the same.");
        }
    }
}
