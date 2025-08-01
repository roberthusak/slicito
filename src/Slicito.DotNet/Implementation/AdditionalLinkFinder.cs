using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Slicito.DotNet.Implementation;

internal class AdditionalLinkFinder(FlowGraphCreator.BlockTranslationContext context) : OperationWalker
{
    public override void VisitInvocation(IInvocationOperation operation)
    {
        DefaultVisit(operation);

        AddCallTarget(operation.TargetMethod);
    }

    public override void VisitObjectCreation(IObjectCreationOperation operation)
    {
        DefaultVisit(operation);

        AddCallTarget(operation.Constructor);
    }

    public override void VisitPropertyReference(IPropertyReferenceOperation operation)
    {
        DefaultVisit(operation);

        if (RoslynHelper.IsPropertyAutoImplemented(operation.Property))
        {
            // The call is reduced just to accesses to the backing field
            return;
        }

        if (operation.Parent is IAssignmentOperation assignment && assignment.Target == operation)
        {
            AddCallTarget(operation.Property.SetMethod);
        }
        else
        {
            AddCallTarget(operation.Property.GetMethod);
        }
    }

    private void AddCallTarget(IMethodSymbol? method)
    {
        if (method is not null)
        {
            var methodElement = context.GetElement(method);
            context.AddCallTarget(methodElement);
        }
    }
}
