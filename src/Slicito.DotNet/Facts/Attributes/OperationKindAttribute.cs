using Slicito.Abstractions.Facts.Attributes;

namespace Slicito.DotNet.Facts.Attributes;

public class OperationKindAttribute(string operationKind) : ElementAttributeAttribute(DotNetAttributeNames.OperationKind, operationKind)
{
}
