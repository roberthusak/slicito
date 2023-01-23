using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class DependencyRelations : IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    internal DependencyRelations(
        IRelation<DotNetType, DotNetType, EmptyStruct> inheritsFrom,
        IRelation<DotNetMethod, DotNetMethod, EmptyStruct> overrides,
        IRelation<DotNetOperation, DotNetMethod, SyntaxNode> calls,
        IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> stores,
        IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> loads,
        IRelation<DotNetElement, DotNetType, SyntaxNode> referencesType,
        IRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> isOfType)
    {
        InheritsFrom = inheritsFrom;
        Overrides = overrides;
        Calls = calls;
        StoresTo = stores;
        LoadsFrom = loads;
        ReferencesType = referencesType;
        IsOfType = isOfType;
    }

    public IRelation<DotNetType, DotNetType, EmptyStruct> InheritsFrom { get; }

    public IRelation<DotNetMethod, DotNetMethod, EmptyStruct> Overrides { get; }

    public IRelation<DotNetOperation, DotNetMethod, SyntaxNode> Calls { get; }

    public IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> StoresTo { get; }

    public IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> LoadsFrom { get; }

    public IRelation<DotNetElement, DotNetType, SyntaxNode> ReferencesType { get; }

    public IRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> IsOfType { get; }

    public IEnumerator<IRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
    {
        yield return InheritsFrom.SetData((SyntaxNode?) null);
        yield return Overrides.SetData((SyntaxNode?) null);
        yield return Calls;
        yield return StoresTo;
        yield return LoadsFrom;
        yield return ReferencesType;
        yield return IsOfType.SetData((SyntaxNode?) null);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
