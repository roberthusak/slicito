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

            private void AddOperationElement(IOperation operation)
            {
                var id = $"{_methodElement.Id}#{operation.Syntax.SpanStart}-{operation.Syntax.Span.End}";

                var element = new DotNetOperation(operation, id);

                _builder._elements.Add(element);
                _builder._hierarchyBuilder.Add(_methodElement, element, default);
            }
        }
    }
}
