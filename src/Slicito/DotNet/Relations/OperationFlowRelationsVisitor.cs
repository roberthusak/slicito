using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class OperationFlowRelationsVisitor : OperationVisitor<DotNetOperation, EmptyStruct>
{
    private readonly DotNetContext _context;
    private readonly FlowRelations.Builder _builder;

    private readonly IBinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct> _isOverridenByRelation;

    public OperationFlowRelationsVisitor(DotNetContext context, FlowRelations.Builder builder)
    {
        _context = context;
        _builder = builder;

        _isOverridenByRelation = builder.DependencyRelations.Overrides.Invert();
    }

    public override EmptyStruct VisitConversion(IConversionOperation operation, DotNetOperation operationElement)
    {
        base.VisitConversion(operation, operationElement);

        HandleRead(operation.Operand, operationElement, isCopy: true);

        return default;
    }

    public override EmptyStruct VisitInvocation(IInvocationOperation operation, DotNetOperation operationElement)
    {
        base.VisitInvocation(operation, operationElement);

        HandleInvocation(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitFieldReference(IFieldReferenceOperation operation, DotNetOperation operationElement)
    {
        base.VisitFieldReference(operation, operationElement);

        HandleMemberReference(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitPropertyReference(IPropertyReferenceOperation operation, DotNetOperation operationElement)
    {
        base.VisitPropertyReference(operation, operationElement);

        HandleMemberReference(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitSimpleAssignment(ISimpleAssignmentOperation operation, DotNetOperation operationElement)
    {
        base.VisitSimpleAssignment(operation, operationElement);

        HandleAssignment(operation, operationElement);

        return default;
    }

    private void HandleInvocation(IInvocationOperation operation, DotNetOperation operationElement)
    {
        var baseCallTargets = _builder.DependencyRelations.Calls
            .GetOutgoing(operationElement)
            .Select(m => m.Target)
            .ToArray();

        var potentialCallTargets = baseCallTargets
            .Concat(_isOverridenByRelation
                .SliceForward(baseCallTargets)
                .GetElements())
            .Where(e => !e.Symbol.IsAbstract)
            .ToHashSet();

        if (operation.Instance is not null
            && _context.TryGetElementFromOperation(operation.Instance) is DotNetOperation instanceElement)
        {
            HandleRead(instanceElement, operationElement, isCopy: false);

            foreach (var targetMethodElement in potentialCallTargets)
            {
                var thisParameter = targetMethodElement.Symbol.Parameters
                    .FirstOrDefault(p => p.IsThis);

                if (thisParameter is not null
                    && _context.TryGetElementFromSymbol(thisParameter) is DotNetParameter thisParameterElement)
                {
                    _builder.IsPassedAs.Add(instanceElement, thisParameterElement, operation.Instance.Syntax);
                }
            }
        }

        foreach (var argument in operation.Arguments)
        {
            var value = argument.Value;

            if (_context.TryGetElementFromOperation(argument) is not DotNetOperation argumentElement
                || _context.TryGetElementFromOperation(value) is not DotNetOperation valueElement)
            {
                continue;
            }

            HandleRead(valueElement, argumentElement, isCopy: true);
            _builder.ResultIsReadBy.Add(argumentElement, operationElement, argument.Syntax);

            var parameterName = argument.Parameter?.Name;
            if (string.IsNullOrEmpty(parameterName))
            {
                continue;
            }

            foreach (var targetMethodElement in potentialCallTargets)
            {
                var parameter = targetMethodElement.Symbol.Parameters
                    .SingleOrDefault(p => p.Name == parameterName);

                if (parameter is null
                    || _context.TryGetElementFromSymbol(parameter) is not DotNetParameter parameterElement)
                {
                    continue;
                }

                _builder.IsPassedAs.Add(argumentElement, parameterElement, argument.Syntax);
            }
        }
    }

    private void HandleMemberReference(IMemberReferenceOperation operation, DotNetOperation operationElement)
    {
        if (operation.Instance is not null
            && _context.TryGetElementFromOperation(operation.Instance) is DotNetOperation instanceElement)
        {
            HandleRead(instanceElement, operationElement, isCopy: false);
        }
    }

    private void HandleAssignment(IAssignmentOperation operation, DotNetOperation operationElement)
    {
        HandleRead(operation.Value, operationElement, isCopy: true);
        HandleWrite(operation.Target, operationElement, isCopy: true);
    }

    private void HandleRead(IOperation? value, DotNetOperation operationElement, bool isCopy)
    {
        if (value is null)
        {
            return;
        }

        HandleRead(_context.TryGetElementFromOperation(value), operationElement, isCopy);
    }

    private void HandleRead(DotNetOperation? valueElement, DotNetOperation operationElement, bool isCopy)
    {
        if (valueElement is null)
        {
            return;
        }

        var relation = isCopy ? _builder.ResultIsCopiedTo : _builder.ResultIsReadBy;

        relation.Add(valueElement, operationElement, valueElement.Operation.Syntax);

        var variableElement = TryGetVariableElementFromReference(valueElement.Operation);
        if (variableElement is not null)
        {
            _builder.VariableIsReadBy.Add(variableElement, valueElement, valueElement.Operation.Syntax);
        }
    }

    private void HandleWrite(IOperation? target, DotNetOperation operationElement, bool isCopy)
    {
        if (target is null)
        {
            return;
        }

        HandleWrite(_context.TryGetElementFromOperation(target), operationElement, isCopy);
    }

    private void HandleWrite(DotNetOperation? targetElement, DotNetOperation operationElement, bool isCopy)
    {
        if (targetElement is null)
        {
            return;
        }

        var relation = isCopy ? _builder.ResultIsCopiedTo : _builder.ResultIsReadBy;

        relation.Add(operationElement, targetElement, targetElement.Operation.Syntax);

        var variableElement = TryGetVariableElementFromReference(targetElement.Operation);
        if (variableElement is not null)
        {
            _builder.WritesToVariable.Add(targetElement, variableElement, targetElement.Operation.Syntax);
        }
    }

    private DotNetVariable? TryGetVariableElementFromReference(IOperation operation)
    {
        ISymbol? variableSymbol = operation switch
        {
            IParameterReferenceOperation { Parameter: var parameterSymbol } =>
                parameterSymbol,
            ILocalReferenceOperation { Local: var localSymbol } =>
                localSymbol,
            _ =>
                null
        };

        if (variableSymbol is null
            || _context.TryGetElementFromSymbol(variableSymbol) is not DotNetVariable variableElement)
        {
            return null;
        }

        return variableElement;
    }
}
