using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
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

    public override EmptyStruct VisitExpressionStatement(IExpressionStatementOperation operation, DotNetOperation operationElement)
    {
        base.VisitExpressionStatement(operation, operationElement);

        HandleRead(operation.Operation, operationElement, isCopy: false);

        return default;
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
        HandleIndexerArguments(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitUnaryOperator(IUnaryOperation operation, DotNetOperation operationElement)
    {
        base.VisitUnaryOperator(operation, operationElement);

        HandleRead(operation.Operand, operationElement, isCopy: false);

        return default;
    }

    public override EmptyStruct VisitBinaryOperator(IBinaryOperation operation, DotNetOperation operationElement)
    {
        base.VisitBinaryOperator(operation, operationElement);

        HandleRead(operation.LeftOperand, operationElement, isCopy: false);
        HandleRead(operation.RightOperand, operationElement, isCopy: false);

        return default;
    }

    public override EmptyStruct VisitObjectCreation(IObjectCreationOperation operation, DotNetOperation operationElement)
    {
        base.VisitObjectCreation(operation, operationElement);

        HandleObjectCreation(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitArrayCreation(IArrayCreationOperation operation, DotNetOperation operationElement)
    {
        base.VisitArrayCreation(operation, operationElement);

        foreach (var dimensionSize in operation.DimensionSizes)
        {
            HandleRead(dimensionSize, operationElement, isCopy: false);
        }

        HandleRead(operation.Initializer, operationElement, isCopy: false);

        return default;
    }

    public override EmptyStruct VisitAwait(IAwaitOperation operation, DotNetOperation operationElement)
    {
        base.VisitAwait(operation, operationElement);

        HandleRead(operation.Operation, operationElement, isCopy: false);

        return default;
    }

    public override EmptyStruct VisitSimpleAssignment(ISimpleAssignmentOperation operation, DotNetOperation operationElement)
    {
        base.VisitSimpleAssignment(operation, operationElement);

        HandleAssignment(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitIsPattern(IIsPatternOperation operation, DotNetOperation operationElement)
    {
        base.VisitIsPattern(operation, operationElement);

        HandleIsPattern(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitArrayInitializer(IArrayInitializerOperation operation, DotNetOperation operationElement)
    {
        base.VisitArrayInitializer(operation, operationElement);

        foreach (var elementValue in operation.ElementValues)
        {
            HandleRead(elementValue, operationElement, isCopy: false);
        }

        return default;
    }

    public override EmptyStruct VisitConstantPattern(IConstantPatternOperation operation, DotNetOperation operationElement)
    {
        base.VisitConstantPattern(operation, operationElement);

        HandleRead(operation.Value, operationElement, isCopy: false);
        
        return default;
    }

    public override EmptyStruct VisitDeclarationPattern(IDeclarationPatternOperation operation, DotNetOperation operationElement)
    {
        base.VisitDeclarationPattern(operation, operationElement);

        HandleDeclarationPattern(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitFlowCapture(IFlowCaptureOperation operation, DotNetOperation operationElement)
    {
        base.VisitFlowCapture(operation, operationElement);

        HandleRead(operation.Value, operationElement, isCopy: true);

        return default;
    }

    public override EmptyStruct VisitIsNull(IIsNullOperation operation, DotNetOperation operationElement)
    {
        base.VisitIsNull(operation, operationElement);

        HandleRead(operation.Operand, operationElement, isCopy: false);

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

        // Connect the instance to the references to the current instance in the called method
        if (operation.Instance is not null
            && _context.TryGetElementFromOperation(operation.Instance) is DotNetOperation instanceElement)
        {
            HandleRead(instanceElement, operationElement, isCopy: false);

            foreach (var targetMethodElement in potentialCallTargets)
            {
                var instanceReferenceElementsQuery = _context.Hierarchy.GetOutgoing(targetMethodElement)
                    .Select(pair => pair.Target)
                    .OfType<DotNetOperation>()
                    .Where(o => o.Operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance });

                foreach (var instanceReferenceElement in instanceReferenceElementsQuery)
                {
                    _builder.ResultIsCopiedTo.Add(instanceElement, instanceReferenceElement, instanceElement.Operation.Syntax);
                }
            }
        }

        HandleArgumentList(operationElement, potentialCallTargets, operation.Arguments);

        // Connect return values to the invocation operation
        foreach (var targetMethodElement in potentialCallTargets)
        {
            if (targetMethodElement.ControlFlowGraph is null)
            {
                continue;
            }

            var exitBlock = targetMethodElement.ControlFlowGraph.Blocks[^1];
            Debug.Assert(exitBlock.Kind == BasicBlockKind.Exit);

            foreach (var returnBranch in exitBlock.Predecessors)
            {
                if (returnBranch.Semantics != ControlFlowBranchSemantics.Return)
                {
                    continue;
                }

                var returnValue = returnBranch.Source.BranchValue;
                if (returnValue is not null
                    && _context.TryGetElementFromOperation(returnValue) is DotNetOperation returnValueElement)
                {
                    _builder.ResultIsCopiedTo.Add(returnValueElement, operationElement, returnValue.Syntax);
                }
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

    private void HandleIndexerArguments(IPropertyReferenceOperation operation, DotNetOperation operationElement)
    {
        HandleArgumentList(operationElement, Enumerable.Empty<DotNetMethod>(), operation.Arguments);
    }

    private void HandleObjectCreation(IObjectCreationOperation operation, DotNetOperation operationElement)
    {
        var callTargets = Array.Empty<DotNetMethod>();

        if (operation.Constructor is not null
            && _context.TryGetElementFromSymbol(operation.Constructor) is DotNetMethod constructorElement)
        {
            callTargets = new[] { constructorElement };
        }

        HandleArgumentList(operationElement, callTargets, operation.Arguments);
    }

    private void HandleAssignment(IAssignmentOperation operation, DotNetOperation operationElement)
    {
        HandleRead(operation.Value, operationElement, isCopy: true);
        HandleWrite(operation.Target, operationElement, isCopy: true);
    }

    private void HandleIsPattern(IIsPatternOperation operation, DotNetOperation operationElement)
    {
        if (_context.TryGetElementFromOperation(operation.Pattern) is not DotNetOperation patternElement)
        {
            return;
        }

        HandleRead(operation.Value, patternElement, isCopy: patternElement.Operation is IDeclarationPatternOperation);

        _builder.ResultIsReadBy.Add(patternElement, operationElement, patternElement.Operation.Syntax);
    }

    private void HandleDeclarationPattern(IDeclarationPatternOperation operation, DotNetOperation operationElement)
    {
        if (operation.DeclaredSymbol is null
            || _context.TryGetElementFromSymbol(operation.DeclaredSymbol) is not DotNetVariable variableElement)
        {
            return;
        }

        _builder.WritesToVariable.Add(operationElement, variableElement, operation.Syntax);
    }

    private void HandleArgumentList(DotNetOperation operationElement, IEnumerable<DotNetMethod> potentialCallTargets, ImmutableArray<IArgumentOperation> arguments)
    {
        foreach (var argument in arguments)
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
            if (argument.Parameter is null || string.IsNullOrEmpty(parameterName))
            {
                continue;
            }

            if (argument.Parameter.RefKind != RefKind.None)
            {
                HandleWrite(valueElement, argumentElement, isCopy: true);
                _builder.ResultIsReadBy.Add(operationElement, argumentElement, argument.Syntax);
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

                if (parameter.RefKind != RefKind.None)
                {
                    _builder.VariableIsReadBy.Add(parameterElement, argumentElement, argument.Syntax);
                }
            }
        }
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

        foreach (var captureOperationElement in GetReferencedCaptureOperations(valueElement))
        {
            relation.Add(captureOperationElement, valueElement, valueElement.Operation.Syntax);
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

        foreach (var captureOperationElement in GetReferencedCaptureOperations(targetElement))
        {
            relation.Add(targetElement, captureOperationElement, targetElement.Operation.Syntax);
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

    private IEnumerable<DotNetOperation> GetReferencedCaptureOperations(DotNetOperation operationElement) {
        if (operationElement.Operation is not IFlowCaptureReferenceOperation captureReferenceOperation)
        {
            return Enumerable.Empty<DotNetOperation>();
        }

        var methodElement = _context.Hierarchy.GetAncestors(operationElement)
            .OfType<DotNetMethod>()
            .First();

        return _context.Hierarchy.GetOutgoing(methodElement)
            .Select(pair => pair.Target)
            .OfType<DotNetOperation>()
            .Where(e =>
                e.Operation is IFlowCaptureOperation captureOperation
                && captureOperation.Id.Equals(captureReferenceOperation.Id));
    }
}
