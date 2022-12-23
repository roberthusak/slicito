using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class OperationDependencyRelationsVisitor : OperationVisitor<DotNetOperation, EmptyStruct>
{
    private readonly DotNetContext _context;
    private readonly DependencyRelations.Builder _builder;

    public OperationDependencyRelationsVisitor(DotNetContext context, DependencyRelations.Builder builder)
    {
        _context = context;
        _builder = builder;
    }

    public override EmptyStruct VisitInvocation(IInvocationOperation operation, DotNetOperation operationElement)
    {
        base.VisitInvocation(operation, operationElement);

        HandleInvocation(operation, operationElement, operation.TargetMethod);

        return default;
    }

    public override EmptyStruct VisitFieldReference(IFieldReferenceOperation operation, DotNetOperation operationElement)
    {
        base.VisitFieldReference(operation, operationElement);

        HandleStorageMemberAccess(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitPropertyReference(IPropertyReferenceOperation operation, DotNetOperation operationElement)
    {
        base.VisitPropertyReference(operation, operationElement);

        HandleStorageMemberAccess(operation, operationElement);

        return default;
    }

    public override EmptyStruct VisitObjectCreation(IObjectCreationOperation operation, DotNetOperation operationElement)
    {
        base.VisitObjectCreation(operation, operationElement);

        HandleInvocation(operation, operationElement, operation.Constructor);

        return default;
    }

    private void HandleInvocation(IOperation operation, DotNetOperation operationElement, IMethodSymbol? targetMethod)
    {
        if (_context.TryGetElementFromSymbol(targetMethod) is not DotNetMethod calleeElement)
        {
            return;
        }

        _builder.Calls.Add(operationElement, calleeElement, operation.Syntax);
    }

    private void HandleStorageMemberAccess(IMemberReferenceOperation operation, DotNetOperation operationElement)
    {
        if (_context.TryGetElementFromSymbol(operation.Member) is not DotNetStorageTypeMember storageElement)
        {
            return;
        }

        if (operation.Parent is IAssignmentOperation assignment && assignment.Target == operation)
        {
            _builder.Stores.Add(operationElement, storageElement, operation.Syntax);
        }
        else
        {
            _builder.Loads.Add(operationElement, storageElement, operation.Syntax);
        }
    }
}
