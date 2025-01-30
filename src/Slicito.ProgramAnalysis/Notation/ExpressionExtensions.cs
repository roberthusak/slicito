namespace Slicito.ProgramAnalysis.Notation;

public static class ExpressionExtensions
{
    public static bool Contains(this Expression expression, Expression other)
    {
        return expression switch
        {
            Expression.UnaryOperator unaryOperator => unaryOperator == other || unaryOperator.Operand.Contains(other),
            Expression.BinaryOperator binaryOperator => binaryOperator == other || binaryOperator.Left.Contains(other) || binaryOperator.Right.Contains(other),
            _ => expression == other,
        };
    }

    public static DataType GetDataType(this Expression expression)
    {
        return expression switch
        {
            Expression.Constant.Boolean _ => DataType.Boolean.Instance,
            Expression.Constant.SignedInteger signedInteger => signedInteger.Type,
            Expression.Constant.UnsignedInteger unsignedInteger => unsignedInteger.Type,
            Expression.Constant.Float @float => @float.Type,
            Expression.Constant.Utf16String _ => DataType.Utf16String.Instance,
            Expression.VariableReference variableReference => variableReference.Variable.Type,
            Expression.UnaryOperator unaryOperator => GetUnaryOperatorDataType(unaryOperator),
            Expression.BinaryOperator binaryOperator => GetBinaryOperatorDataType(binaryOperator),
            _ => throw new InvalidOperationException($"Expression of type '{expression.GetType().Name}' is not supported."),
        };
    }

    private static DataType GetUnaryOperatorDataType(Expression.UnaryOperator unaryOperator)
    {
        return unaryOperator.Kind switch
        {
            UnaryOperatorKind.Negate => unaryOperator.Operand.GetDataType(),
            UnaryOperatorKind.Not => DataType.Boolean.Instance,
            UnaryOperatorKind.StringLength => new DataType.Integer(true, 32),
            _ => throw new InvalidOperationException($"Unary operator of kind '{unaryOperator.Kind}' is not supported."),
        };
    }

    private static DataType GetBinaryOperatorDataType(Expression.BinaryOperator binaryOperator)
    {
        return binaryOperator.Kind switch
        {
            BinaryOperatorKind.Add => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.Subtract => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.Multiply => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.Divide => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.Modulo => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.And => DataType.Boolean.Instance,
            BinaryOperatorKind.Or => DataType.Boolean.Instance,
            BinaryOperatorKind.Xor => DataType.Boolean.Instance,
            BinaryOperatorKind.ShiftLeft => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.ShiftRight => binaryOperator.Left.GetDataType(),
            BinaryOperatorKind.Equal => DataType.Boolean.Instance,
            BinaryOperatorKind.NotEqual => DataType.Boolean.Instance,
            BinaryOperatorKind.LessThan => DataType.Boolean.Instance,
            BinaryOperatorKind.LessThanOrEqual => DataType.Boolean.Instance,
            BinaryOperatorKind.GreaterThan => DataType.Boolean.Instance,
            BinaryOperatorKind.GreaterThanOrEqual => DataType.Boolean.Instance,
            BinaryOperatorKind.StringStartsWith => DataType.Boolean.Instance,
            BinaryOperatorKind.StringEndsWith => DataType.Boolean.Instance,
            _ => throw new InvalidOperationException($"Binary operator '{binaryOperator.Kind}' is not supported."),
        };
    }
}
