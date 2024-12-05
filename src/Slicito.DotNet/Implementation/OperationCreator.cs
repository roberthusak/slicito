using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

using Slicito.ProgramAnalysis.Notation;

using RoslynBinaryOperatorKind = Microsoft.CodeAnalysis.Operations.BinaryOperatorKind;

using SlicitoBinaryOperatorKind = Slicito.ProgramAnalysis.Notation.BinaryOperatorKind;
using SlicitoLocation = Slicito.ProgramAnalysis.Notation.Location;

namespace Slicito.DotNet.Implementation;

internal class OperationCreator(FlowGraphCreator.BlockTranslationContext context) : OperationVisitor<Empty, Expression>
{
    public override Expression? DefaultVisit(IOperation operation, Empty _)
    {
        throw new NotSupportedException($"Operation {operation.Kind} (type: {operation.GetType().Name}) is not supported.");
    }

    public override Expression? VisitExpressionStatement(IExpressionStatementOperation operation, Empty _)
    {
        return operation.Operation.Accept(this, default);
    }

    public override Expression? VisitLiteral(ILiteralOperation operation, Empty _)
    {
        Debug.Assert(operation.ConstantValue.HasValue);

        return operation.ConstantValue.Value switch
        {
            bool b => new Expression.Constant.Boolean(b),
            sbyte i => new Expression.Constant.SignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_SByte)),
            byte i => new Expression.Constant.UnsignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_Byte)),
            short i => new Expression.Constant.SignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_Int16)),
            ushort i => new Expression.Constant.UnsignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_UInt16)),
            int i => new Expression.Constant.SignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_Int32)),
            uint i => new Expression.Constant.UnsignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_UInt32)),
            long i => new Expression.Constant.SignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_Int64)),
            ulong i => new Expression.Constant.UnsignedInteger(i, (DataType.Integer) TypeCreator.Create(SpecialType.System_UInt64)),
            float f => new Expression.Constant.Float(f, (DataType.Float) TypeCreator.Create(SpecialType.System_Single)),
            double d => new Expression.Constant.Float(d, (DataType.Float) TypeCreator.Create(SpecialType.System_Double)),
            _ => throw new NotSupportedException($"Unsupported literal type: {operation.ConstantValue.Value?.GetType().Name ?? "null"}."),
        };
    }

    public override Expression? VisitParameterReference(IParameterReferenceOperation operation, Empty _)
    {
        var variable = context.GetOrCreateVariable(operation.Parameter);

        return new Expression.VariableReference(variable);
    }

    public override Expression? VisitLocalReference(ILocalReferenceOperation operation, Empty _)
    {
        var variable = context.GetOrCreateVariable(operation.Local);

        return new Expression.VariableReference(variable);
    }

    public override Expression? VisitSimpleAssignment(ISimpleAssignmentOperation operation, Empty _)
    {
        var variable = operation.Target switch
        {
            ILocalReferenceOperation localReferenceOperation => context.GetOrCreateVariable(localReferenceOperation.Local),
            IParameterReferenceOperation parameterReferenceOperation => context.GetOrCreateVariable(parameterReferenceOperation.Parameter),
            _ => throw new NotSupportedException($"Unsupported target of a simple assignment operation: {operation.Target.GetType().Name}."),
        };

        var value = VisitEnsureNonNull(operation.Value);

        context.AddInnerOperation(
            new Operation.Assignment(
                new SlicitoLocation.VariableReference(variable),
                value),
            operation.Syntax);

        return new Expression.VariableReference(variable);
    }

    public override Expression? VisitBinaryOperator(IBinaryOperation operation, Empty _)
    {
        var left = VisitEnsureNonNull(operation.LeftOperand);
        var right = VisitEnsureNonNull(operation.RightOperand);

        return new Expression.BinaryOperator(TranslateBinaryOperatorKind(operation.OperatorKind), left, right);
    }

    public override Expression? VisitInvocation(IInvocationOperation operation, Empty _)
    {
        var signature = ProcedureSignatureCreator.Create(operation.TargetMethod);

        var arguments = operation.Arguments
            .Select(a => VisitEnsureNonNull(a.Value))
            .ToImmutableArray();

        var returnLocations = signature.ReturnTypes
            .Select(t => (SlicitoLocation?) new SlicitoLocation.VariableReference(context.CreateTemporaryVariable(t)))
            .ToImmutableArray();

        context.AddInnerOperation(
            new Operation.Call(
                signature,
                arguments,
                returnLocations),
            operation.Syntax);

        // If the return type is void, this operation should be a separate statement, so returning null is safe
        return returnLocations.IsEmpty || returnLocations[0] is not SlicitoLocation.VariableReference returnVariableReference
            ? null
            : new Expression.VariableReference(returnVariableReference.Variable);
    }

    private static SlicitoBinaryOperatorKind TranslateBinaryOperatorKind(RoslynBinaryOperatorKind operatorKind)
    {
        return operatorKind switch
        {
            RoslynBinaryOperatorKind.Add => SlicitoBinaryOperatorKind.Add,
            RoslynBinaryOperatorKind.Subtract => SlicitoBinaryOperatorKind.Subtract,
            RoslynBinaryOperatorKind.Multiply => SlicitoBinaryOperatorKind.Multiply,
            RoslynBinaryOperatorKind.Divide => SlicitoBinaryOperatorKind.Divide,
            RoslynBinaryOperatorKind.Remainder => SlicitoBinaryOperatorKind.Modulo,
            RoslynBinaryOperatorKind.And => SlicitoBinaryOperatorKind.And,
            RoslynBinaryOperatorKind.Or => SlicitoBinaryOperatorKind.Or,
            RoslynBinaryOperatorKind.ExclusiveOr => SlicitoBinaryOperatorKind.Xor,
            RoslynBinaryOperatorKind.LeftShift => SlicitoBinaryOperatorKind.ShiftLeft,
            RoslynBinaryOperatorKind.RightShift => SlicitoBinaryOperatorKind.ShiftRight,
            RoslynBinaryOperatorKind.Equals => SlicitoBinaryOperatorKind.Equal,
            RoslynBinaryOperatorKind.NotEquals => SlicitoBinaryOperatorKind.NotEqual,
            RoslynBinaryOperatorKind.LessThan => SlicitoBinaryOperatorKind.LessThan,
            RoslynBinaryOperatorKind.LessThanOrEqual => SlicitoBinaryOperatorKind.LessThanOrEqual,
            RoslynBinaryOperatorKind.GreaterThan => SlicitoBinaryOperatorKind.GreaterThan,
            RoslynBinaryOperatorKind.GreaterThanOrEqual => SlicitoBinaryOperatorKind.GreaterThanOrEqual,
            _ => throw new NotSupportedException($"Unsupported binary operator kind: {operatorKind}."),
        };
    }

    private Expression VisitEnsureNonNull(
        IOperation operation,
        [CallerMemberName] string? callerName = null)
    {
        return operation.Accept(this, default)
            ?? throw new NotSupportedException(
                $"Visiting operation {operation.Kind} (type: {operation.GetType().Name}) in {callerName} returned null.");
    }
}
