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
            Expression.Constant.Float floatConst => floatConst.Value.ToString(),
            Expression.Constant.Utf16String stringConst => $"\"{stringConst.Value}\"",
            Expression.Constant.StringPattern patternConst => $"\"{patternConst.Pattern}\"",
            Expression.UnaryOperator unaryOp => FormatUnaryOperator(unaryOp),
            Expression.BinaryOperator binOp => FormatBinaryOperator(binOp),
            _ => throw new ArgumentException($"Unsupported expression type {expression.GetType().Name}.")
        };
    }

    private static string FormatUnaryOperator(Expression.UnaryOperator @operator)
    {
        return @operator.Kind switch
        {
            UnaryOperatorKind.Negate => $"-{@operator.Operand.Format()}",
            UnaryOperatorKind.Not => $"!{@operator.Operand.Format()}",
            UnaryOperatorKind.StringLength => $"len({@operator.Operand.Format()})",
            _ => throw new ArgumentException($"Unsupported unary operator kind {@operator.Kind}.")
        };
    }

    private static string FormatBinaryOperator(Expression.BinaryOperator op)
    {
        return op.Kind switch
        {
            BinaryOperatorKind.Add => FormatInfix("+"),
            BinaryOperatorKind.Subtract => FormatInfix("-"),
            BinaryOperatorKind.Multiply => FormatInfix("*"),
            BinaryOperatorKind.Divide => FormatInfix("/"),
            BinaryOperatorKind.Modulo => FormatInfix("%"),
            BinaryOperatorKind.Equal => FormatInfix("=="),
            BinaryOperatorKind.GreaterThan => FormatInfix(">"),
            BinaryOperatorKind.LessThan => FormatInfix("<"),
            BinaryOperatorKind.GreaterThanOrEqual => FormatInfix(">="),
            BinaryOperatorKind.LessThanOrEqual => FormatInfix("<="),
            BinaryOperatorKind.NotEqual => FormatInfix("!="),
            BinaryOperatorKind.And => FormatInfix("&"),
            BinaryOperatorKind.Or => FormatInfix("|"),
            BinaryOperatorKind.Xor => FormatInfix("^"),
            BinaryOperatorKind.ShiftLeft => FormatInfix(">>"),
            BinaryOperatorKind.ShiftRight => FormatInfix(">>"),
            BinaryOperatorKind.StringStartsWith => $"startsWith({op.Left.Format()}, {op.Right.Format()})",
            BinaryOperatorKind.StringEndsWith => $"endsWith({op.Left.Format()}, {op.Right.Format()})",
            BinaryOperatorKind.StringMatchesPattern => $"matches({op.Left.Format()}, {op.Right.Format()})",
            _ => throw new ArgumentException($"Unsupported binary operator kind {op.Kind}.")
        };

        string FormatInfix(string operatorSymbol)
        {
            return $"{op.Left.Format()} {operatorSymbol} {op.Right.Format()}";
        }
    }
}
