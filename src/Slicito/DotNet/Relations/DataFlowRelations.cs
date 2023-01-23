using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class DataFlowRelations : IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    private DataFlowRelations(
        Relation<DotNetVariable, DotNetOperation, SyntaxNode> variableIsReadBy,
        Relation<DotNetOperation, DotNetVariable, SyntaxNode> writesToVariable,
        Relation<DotNetOperation, DotNetOperation, SyntaxNode> resultIsCopiedTo,
        Relation<DotNetOperation, DotNetOperation, SyntaxNode> resultIsReadBy,
        Relation<DotNetOperation, DotNetParameter, SyntaxNode> isPassedAs,
        IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> stores,
        IRelation<DotNetStorageTypeMember, DotNetOperation, SyntaxNode> isLoadedTo)
    {
        VariableIsReadBy = variableIsReadBy;
        WritesToVariable = writesToVariable;
        ResultIsCopiedTo = resultIsCopiedTo;
        ResultIsReadBy = resultIsReadBy;
        IsPassedAs = isPassedAs;
        Stores = stores;
        IsLoadedTo = isLoadedTo;
    }

    public Relation<DotNetVariable, DotNetOperation, SyntaxNode> VariableIsReadBy { get; }

    public Relation<DotNetOperation, DotNetVariable, SyntaxNode> WritesToVariable { get; }

    public Relation<DotNetOperation, DotNetOperation, SyntaxNode> ResultIsCopiedTo { get; }

    public Relation<DotNetOperation, DotNetOperation, SyntaxNode> ResultIsReadBy { get; }

    public Relation<DotNetOperation, DotNetParameter, SyntaxNode> IsPassedAs { get; }

    public IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> Stores { get; }

    public IRelation<DotNetStorageTypeMember, DotNetOperation, SyntaxNode> IsLoadedTo { get; }

    public IEnumerator<IRelation<DotNetElement, DotNetElement, SyntaxNode>> GetEnumerator()
    {
        yield return VariableIsReadBy;
        yield return WritesToVariable;
        yield return ResultIsCopiedTo;
        yield return ResultIsReadBy;
        yield return IsPassedAs;
        yield return Stores;
        yield return IsLoadedTo;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode>> GetValueFlow()
    {
        yield return VariableIsReadBy;
        yield return WritesToVariable;
        yield return ResultIsCopiedTo;
        yield return IsPassedAs;
        yield return Stores;
        yield return IsLoadedTo;
    }
}
