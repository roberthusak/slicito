using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class DependencyRelations : IEnumerable<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    internal DependencyRelations(
        IBinaryRelation<DotNetType, DotNetType, EmptyStruct> inheritsFrom,
        IBinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct> overrides,
        IBinaryRelation<DotNetOperation, DotNetMethod, SyntaxNode> calls,
        IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> stores,
        IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> loads,
        IBinaryRelation<DotNetElement, DotNetType, SyntaxNode> referencesType,
        IBinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> isOfType)
    {
        InheritsFrom = inheritsFrom;
        Overrides = overrides;
        Calls = calls;
        StoresTo = stores;
        LoadsFrom = loads;
        ReferencesType = referencesType;
        IsOfType = isOfType;
    }

    public IBinaryRelation<DotNetType, DotNetType, EmptyStruct> InheritsFrom { get; }

    public IBinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct> Overrides { get; }

    public IBinaryRelation<DotNetOperation, DotNetMethod, SyntaxNode> Calls { get; }

    public IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> StoresTo { get; }

    public IBinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> LoadsFrom { get; }

    public IBinaryRelation<DotNetElement, DotNetType, SyntaxNode> ReferencesType { get; }

    public IBinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> IsOfType { get; }

    public IEnumerator<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
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
