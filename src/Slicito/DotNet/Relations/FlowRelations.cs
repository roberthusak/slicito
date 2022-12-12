using System.Collections;

using Microsoft.CodeAnalysis;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class FlowRelations : IEnumerable<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    private FlowRelations(
        BinaryRelation<DotNetVariable, DotNetOperation, SyntaxNode> variableIsReadBy,
        BinaryRelation<DotNetOperation, DotNetVariable, SyntaxNode> writesToVariable,
        BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode> resultIsReadBy,
        BinaryRelation<DotNetOperation, DotNetParameter, SyntaxNode> isPassedAs,
        IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> stores,
        IBinaryRelation<DotNetStorageTypeMember, DotNetOperation, SyntaxNode> isLoadedTo)
    {
        VariableIsReadBy = variableIsReadBy;
        WritesToVariable = writesToVariable;
        ResultIsReadBy = resultIsReadBy;
        IsPassedAs = isPassedAs;
        Stores = stores;
        IsLoadedTo = isLoadedTo;
    }

    public BinaryRelation<DotNetVariable, DotNetOperation, SyntaxNode> VariableIsReadBy { get; }

    public BinaryRelation<DotNetOperation, DotNetVariable, SyntaxNode> WritesToVariable { get; }

    public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode> ResultIsReadBy { get; }

    public BinaryRelation<DotNetOperation, DotNetParameter, SyntaxNode> IsPassedAs { get; }

    public IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> Stores { get; }

    public IBinaryRelation<DotNetStorageTypeMember, DotNetOperation, SyntaxNode> IsLoadedTo { get; }

    public IEnumerator<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode>> GetEnumerator()
    {
        yield return VariableIsReadBy;
        yield return WritesToVariable;
        yield return ResultIsReadBy;
        yield return IsPassedAs;
        yield return Stores;
        yield return IsLoadedTo;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
