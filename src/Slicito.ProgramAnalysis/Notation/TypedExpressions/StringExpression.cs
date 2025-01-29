namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

public readonly struct StringExpression
{
    public Expression Expression { get; }

    public StringExpression(Expression expression)
    {
        Expression = expression;

        if (expression.GetDataType() is not DataType.Utf16String)
        {
            throw new InvalidOperationException("The expression is not a string.");
        }
    }

    public static implicit operator StringExpression(string value)
    {
        return new StringExpression(new Expression.Constant.Utf16String(value));
    }

    public IntegerExpression Length =>
        new(new Expression.UnaryOperator(UnaryOperatorKind.StringLength, Expression));

    public BooleanExpression StartsWith(StringExpression prefix) =>
        new(new Expression.BinaryOperator(BinaryOperatorKind.StringStartsWith, Expression, prefix.Expression));

    public BooleanExpression EndsWith(StringExpression suffix) =>
        new(new Expression.BinaryOperator(BinaryOperatorKind.StringEndsWith, Expression, suffix.Expression));
}