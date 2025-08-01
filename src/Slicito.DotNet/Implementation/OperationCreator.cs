using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

using Slicito.ProgramAnalysis.Notation;

using RoslynBinaryOperatorKind = Microsoft.CodeAnalysis.Operations.BinaryOperatorKind;

using SlicitoUnaryOperatorKind = Slicito.ProgramAnalysis.Notation.UnaryOperatorKind;
using SlicitoBinaryOperatorKind = Slicito.ProgramAnalysis.Notation.BinaryOperatorKind;
using SlicitoLocation = Slicito.ProgramAnalysis.Notation.Location;
using System.Text.RegularExpressions;

namespace Slicito.DotNet.Implementation;

internal class OperationCreator(FlowGraphCreator.BlockTranslationContext context) : OperationVisitor<Empty, Expression>
{
    private readonly AdditionalLinkFinder _additionalLinkFinder = new(context);

    public override Expression? DefaultVisit(IOperation operation, Empty _)
    {
        if (operation.ConstantValue.HasValue)
        {
            return TranslateConstantValue(operation.ConstantValue.Value);
        }

        _additionalLinkFinder.Visit(operation);

        return new Expression.Unsupported($"Operation: {operation.Kind} (type: {operation.GetType().Name})");
    }

    public override Expression? VisitExpressionStatement(IExpressionStatementOperation operation, Empty _)
    {
        return operation.Operation.Accept(this, default);
    }

    public override Expression? VisitLiteral(ILiteralOperation operation, Empty _)
    {
        Debug.Assert(operation.ConstantValue.HasValue);

        return TranslateConstantValue(operation.ConstantValue.Value);
    }

    public override Expression? VisitConversion(IConversionOperation operation, Empty _)
    {
        if (operation.Type?.Equals(operation.Operand.Type, SymbolEqualityComparer.IncludeNullability) == true)
        {
            return VisitEnsureNonNull(operation.Operand);
        }

        return DefaultVisit(operation, default);
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
            _ => null,
        };

        if (variable is null)
        {
            _additionalLinkFinder.Visit(operation.Target);

            return new Expression.Unsupported($"Assignment target: {operation.Target.Kind} (type: {operation.Target.GetType().Name})");
        }

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

        var kind = TranslateBinaryOperatorKind(operation.OperatorKind);
        if (kind is null)
        {
            return new Expression.Unsupported($"Binary operator: {operation.OperatorKind}");
        }

        return new Expression.BinaryOperator(kind.Value, left, right);
    }

    public override Expression? VisitPropertyReference(IPropertyReferenceOperation operation, Empty _)
    {
        if (operation.Instance is not null)
        {
            var instance = VisitEnsureNonNull(operation.Instance);
    
            if (operation.Instance.Type is { SpecialType: SpecialType.System_String }
                && operation.Property.Name == nameof(string.Length))
            {
                return new Expression.UnaryOperator(SlicitoUnaryOperatorKind.StringLength, instance);
            }
        }

        // Provide at least as a separate operation so that it can be analyzed better
        var unsupportedValue = DefaultVisit(operation, default)!;
        var type = TypeCreator.Create(operation.Property.Type);
        var temporaryVariable = context.CreateTemporaryVariable(type);

        context.AddInnerOperation(
            new Operation.Assignment(
                new SlicitoLocation.VariableReference(temporaryVariable),
                unsupportedValue),
            operation.Syntax);

        return new Expression.VariableReference(temporaryVariable);
    }

    public override Expression? VisitInvocation(IInvocationOperation operation, Empty _)
    {
        var instance = operation.Instance is not null ? VisitEnsureNonNull(operation.Instance) : null;
        
        IEnumerable<Expression> instanceEnumerable = instance is not null ? [instance] : [];

        var arguments = instanceEnumerable
            .Concat(operation.Arguments.Select(a => VisitEnsureNonNull(a.Value)))
            .ToImmutableArray();

        if (instance is not null)
        {
            if (arguments.Length == 2
                && operation.Instance!.Type is { SpecialType: SpecialType.System_String })
            {
                if (operation.TargetMethod.Name == nameof(string.StartsWith))
                {
                    return new Expression.BinaryOperator(SlicitoBinaryOperatorKind.StringStartsWith, instance, arguments[1]);
                }
                else if (operation.TargetMethod.Name == nameof(string.EndsWith))
                {
                    return new Expression.BinaryOperator(SlicitoBinaryOperatorKind.StringEndsWith, instance, arguments[1]);
                }
            }
        }

        if (instance is null
            && operation.TargetMethod.Name == nameof(Regex.IsMatch)
            && arguments.Length == 2
            && RoslynHelper.GetFullName(operation.TargetMethod.ContainingType) == "System.Text.RegularExpressions.Regex")
        {
            Expression pattern;
            try
            {
                pattern = TranslateRegex(arguments[1]);
            }
            catch (NotSupportedException e)
            {
                return new Expression.Unsupported($"Regex: {e.Message}");
            }

            return new Expression.BinaryOperator(SlicitoBinaryOperatorKind.StringMatchesPattern, arguments[0], pattern);
        }

        var targetMethodId = context.GetElement(operation.TargetMethod).Id;
        var signature = ProcedureSignatureCreator.Create(operation.TargetMethod, targetMethodId);

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

    public override Expression? VisitObjectCreation(IObjectCreationOperation operation, Empty argument)
    {
        var constructor = operation.Constructor;
        if (constructor is null)
        {
            return new Expression.Unsupported("Object creation without referenced constructor.");
        }

        var arguments = operation.Arguments
            .Select(a => VisitEnsureNonNull(a.Value))
            .ToImmutableArray();

        var targetMethodId = context.GetElement(constructor).Id;
        var signature = ProcedureSignatureCreator.Create(constructor, targetMethodId);

        var returnType = signature.ReturnTypes.Single();

        var returnLocation = new SlicitoLocation.VariableReference(context.CreateTemporaryVariable(returnType));

        context.AddInnerOperation(
            new Operation.Call(
                signature,
                arguments,
                [returnLocation]),
            operation.Syntax);

        return new Expression.VariableReference(returnLocation.Variable);
    }

    public override Expression? VisitAwait(IAwaitOperation operation, Empty _)
    {
        return Visit(operation.Operation, default);
    }

    private static Expression TranslateConstantValue(object? value)
    {
        return value switch
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

            string s => new Expression.Constant.Utf16String(s),

            _ => new Expression.Unsupported($"Literal type: {value?.GetType().Name ?? "null"}"),
        };
    }

    private static SlicitoBinaryOperatorKind? TranslateBinaryOperatorKind(RoslynBinaryOperatorKind operatorKind)
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
            _ => null,
        };
    }

    private Expression TranslateRegex(Expression expression)
    {
        if (expression is not Expression.Constant.Utf16String stringConstant)
        {
            return new Expression.Unsupported($"Regex pattern: {expression.GetType().Name}.");
        }

        var pattern = StringPatternCreator.ParseRegex(stringConstant.Value);

        return new Expression.Constant.StringPattern(pattern);
    }

    private Expression VisitEnsureNonNull(
        IOperation operation,
        [CallerMemberName] string? callerName = null)
    {
        // Would return null and it's not possible to override it
        if (operation.Kind == OperationKind.None)
        {
            return new Expression.Unsupported($"Operation not supported by Roslyn (syntax kind: {operation.Syntax.Kind()}).");
        }

        return operation.Accept(this, default)
            ?? throw new InvalidOperationException(
                $"Visiting operation {operation.Kind} (type: {operation.GetType().Name}) in {callerName} returned null.");
    }
}
