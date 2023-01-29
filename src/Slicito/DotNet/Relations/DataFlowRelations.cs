using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record DataFlowRelations(
    Relation<DotNetVariable, DotNetOperation, SyntaxNode> VariableIsReadBy,
    Relation<DotNetOperation, DotNetVariable, SyntaxNode> WritesToVariable,
    Relation<DotNetOperation, DotNetOperation, SyntaxNode> ResultIsCopiedTo,
    Relation<DotNetOperation, DotNetOperation, SyntaxNode> ResultIsReadBy,
    Relation<DotNetOperation, DotNetParameter, SyntaxNode> IsPassedAs,
    IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> StoresTo,
    IRelation<DotNetStorageTypeMember, DotNetOperation, SyntaxNode> IsLoadedTo)
    : IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode>>
{
    public IEnumerator<IRelation<DotNetElement, DotNetElement, SyntaxNode>> GetEnumerator()
    {
        yield return VariableIsReadBy;
        yield return WritesToVariable;
        yield return ResultIsCopiedTo;
        yield return ResultIsReadBy;
        yield return IsPassedAs;
        yield return StoresTo;
        yield return IsLoadedTo;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode>> GetValueFlow()
    {
        yield return VariableIsReadBy;
        yield return WritesToVariable;
        yield return ResultIsCopiedTo;
        yield return IsPassedAs;
        yield return StoresTo;
        yield return IsLoadedTo;
    }
}
