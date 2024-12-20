using Slicito.ProgramAnalysis.Notation;

namespace Slicito.Common;

public static class ExpressionExtensions
{
    public static string Format(this Expression expression)
    {
        return expression switch
        {
            Expression.VariableReference varRef => varRef.Variable.Name,
            Expression.Constant.Boolean boolConst => boolConst.Value.ToString(),
            Expression.Constant.SignedInteger intConst => intConst.Value.ToString(),
            Expression.Constant.UnsignedInteger uintConst => uintConst.Value.ToString(),
            Expression.BinaryOperator binOp => $"{binOp.Left.Format()} {binOp.Kind.Format()} {binOp.Right.Format()}",
            _ => throw new ArgumentException($"Unsupported expression type {expression.GetType().Name}.")
        };
    }

    public static string Format(this BinaryOperatorKind kind)
    {
        return kind switch
        {
            BinaryOperatorKind.Add => "+",
            BinaryOperatorKind.Subtract => "-",
            BinaryOperatorKind.Multiply => "*",
            BinaryOperatorKind.Equal => "==",
            BinaryOperatorKind.GreaterThan => ">",
            BinaryOperatorKind.LessThan => "<",
            BinaryOperatorKind.GreaterThanOrEqual => ">=",
            BinaryOperatorKind.LessThanOrEqual => "<=",
            BinaryOperatorKind.NotEqual => "!=",
            BinaryOperatorKind.And => "&",
            BinaryOperatorKind.Or => "|",
            BinaryOperatorKind.Xor => "^",
            BinaryOperatorKind.ShiftLeft => "<<",
            BinaryOperatorKind.ShiftRight => ">>",
            _ => throw new ArgumentException($"Unsupported binary operator kind {kind}.")
        };
    }
}