using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

using Slicito.DotNet.Elements;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public partial class Builder
    {
        private class OperationBuilder : OperationWalker
        {
            private readonly Builder _builder;
            private readonly DotNetMethod _methodElement;

            private int _operationIndex = 0;

            public OperationBuilder(Builder builder, DotNetMethod methodElement)
            {
                _builder = builder;
                _methodElement = methodElement;
            }

            public override void VisitInvocation(IInvocationOperation operation)
            {
                base.VisitInvocation(operation);

                AddOperationElement(operation);
            }

            public override void VisitLocalReference(ILocalReferenceOperation operation)
            {
                base.VisitLocalReference(operation);

                AddOperationElement(operation);
            }

            public override void VisitParameterReference(IParameterReferenceOperation operation)
            {
                base.VisitParameterReference(operation);

                AddOperationElement(operation);
            }

            public override void VisitFieldReference(IFieldReferenceOperation operation)
            {
                base.VisitFieldReference(operation);

                AddOperationElement(operation);
            }

            public override void VisitPropertyReference(IPropertyReferenceOperation operation)
            {
                base.VisitPropertyReference(operation);

                AddOperationElement(operation);
            }

            public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
            {
                base.VisitSimpleAssignment(operation);

                AddOperationElement(operation);
            }

            private void AddOperationElement(IOperation operation)
            {
                var id = $"{_methodElement.Id}#{operation.Kind}:{_operationIndex}";
                _operationIndex++;

                var element = new DotNetOperation(operation, id);

                _builder._elements.Add(element);
                _builder._operationsToElements.Add(operation, element);
                _builder._hierarchyBuilder.Add(_methodElement, element, default);
            }
        }
    }
}
