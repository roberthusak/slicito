using System.Collections.Immutable;

using Slicito.ProgramAnalysis.Notation;

namespace Controllers;

public static class BlockLabelProvider
{
    public static string GetLabel(BasicBlock block)
    {
        return block switch
        {
            BasicBlock.Entry entry => $"Entry({FormatParameters(entry.Parameters)})",
            BasicBlock.Exit => "Exit",
            BasicBlock.Inner inner => GetInnerBlockLabel(inner),
            _ => throw new ArgumentException($"Unsupported block type {block.GetType().Name}.")
        };
    }

    private static string FormatParameters(ImmutableArray<Variable> parameters) =>
        string.Join(", ", parameters.Select(p => p.Name));

    private static string GetInnerBlockLabel(BasicBlock.Inner block)
    {
        if (block.Operation is null)
        {
            return "Empty";
        }

        return block.Operation switch
        {
            Operation.Assignment assignment => FormatAssignment(assignment),
            Operation.ConditionalJump condition => FormatCondition(condition),
            _ => throw new ArgumentException($"Unsupported operation type {block.Operation.GetType().Name}.")
        };
    }

    private static string FormatAssignment(Operation.Assignment assignment)
    {
        var location = assignment.Location switch
        {
            Location.VariableReference varRef => varRef.Variable.Name,
            _ => throw new ArgumentException($"Unsupported location type {assignment.Location.GetType().Name}.")
        };

        return $"{location} = {FormatExpression(assignment.Value)}";
    }

    private static string FormatCondition(Operation.ConditionalJump condition)
    {
        return FormatExpression(condition.Condition);
    }

    private static string FormatExpression(Expression expr)
    {
        return expr switch
        {
            Expression.VariableReference varRef => varRef.Variable.Name,
            Expression.Constant.SignedInteger intConst => intConst.Value.ToString(),
            Expression.Constant.UnsignedInteger uintConst => uintConst.Value.ToString(),
            Expression.BinaryOperator binOp => $"{FormatExpression(binOp.Left)} {FormatBinaryOperator(binOp.Kind)} {FormatExpression(binOp.Right)}",
            _ => throw new ArgumentException($"Unsupported expression type {expr.GetType().Name}.")
        };
    }

    private static string FormatBinaryOperator(BinaryOperatorKind kind) => kind switch
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
