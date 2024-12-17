namespace Slicito.ProgramAnalysis.Notation;

public static class ExpressionExtensions
{
    public static bool Contains(this Expression expression, Expression other)
    {
        return expression switch
        {
            Expression.BinaryOperator binaryOperator => binaryOperator == other || binaryOperator.Left.Contains(other) || binaryOperator.Right.Contains(other),
            _ => expression == other,
        };
    }
}
