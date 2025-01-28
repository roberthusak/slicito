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

    public static BooleanExpression operator &(BooleanExpression left, BooleanExpression right) =>
        CreateOperatorExpression(BinaryOperatorKind.And, left, right);

    public static BooleanExpression operator |(BooleanExpression left, BooleanExpression right) =>
        CreateOperatorExpression(BinaryOperatorKind.Or, left, right);

    public static BooleanExpression operator ^(BooleanExpression left, BooleanExpression right) =>
        CreateOperatorExpression(BinaryOperatorKind.Xor, left, right);

    private static BooleanExpression CreateOperatorExpression(BinaryOperatorKind kind, BooleanExpression left, BooleanExpression right)
    {
        return new BooleanExpression(
            new Expression.BinaryOperator(
                kind,
                left.Expression,
                right.Expression
            )
        );
    }

    public static implicit operator BooleanExpression(bool value)
    {
        return new BooleanExpression(new Expression.Constant.Boolean(value));
    }
}
