namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

public readonly struct BooleanExpression
{
    public Expression Expression { get; }

    public BooleanExpression(Expression expression)
    {
        Expression = expression;

        if (expression.GetDataType() is not DataType.Boolean)
        {
            throw new InvalidOperationException("The expression is not boolean.");
        }
    }

    public static implicit operator BooleanExpression(bool value)
    {
        return new BooleanExpression(new Expression.Constant.Boolean(value));
    }

    public static bool operator true(BooleanExpression _)
    {
        // Disables short-circuit evaluation of ||
        return false;
    }

    public static bool operator false(BooleanExpression _)
    {
        // Disables short-circuit evaluation of &&
        return false;
    }

    public static BooleanExpression operator !(BooleanExpression expression) =>
        new BooleanExpression(new Expression.UnaryOperator(UnaryOperatorKind.Not, expression.Expression));


    public static BooleanExpression operator &(BooleanExpression left, BooleanExpression right) =>
        CreateBinaryOperatorExpression(BinaryOperatorKind.And, left, right);

    public static BooleanExpression operator |(BooleanExpression left, BooleanExpression right) =>
        CreateBinaryOperatorExpression(BinaryOperatorKind.Or, left, right);

    public static BooleanExpression operator ^(BooleanExpression left, BooleanExpression right) =>
        CreateBinaryOperatorExpression(BinaryOperatorKind.Xor, left, right);

    private static BooleanExpression CreateBinaryOperatorExpression(BinaryOperatorKind kind, BooleanExpression left, BooleanExpression right)
    {
        return new BooleanExpression(
            new Expression.BinaryOperator(
                kind,
                left.Expression,
                right.Expression
            )
        );
    }
}
