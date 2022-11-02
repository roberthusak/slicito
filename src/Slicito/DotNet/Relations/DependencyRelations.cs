using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public class DependencyRelations : IEnumerable<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    internal DependencyRelations(
        IBinaryRelation<DotNetType, DotNetType, EmptyStruct> inheritsFrom,
        IBinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct> overrides,
        IBinaryRelation<DotNetMethod, DotNetMethod, SyntaxNode> calls,
        IBinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode> stores,
        IBinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode> loads,
        IBinaryRelation<DotNetElement, DotNetType, SyntaxNode> referencesType,
        IBinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> isOfType)
    {
        InheritsFrom = inheritsFrom;
        Overrides = overrides;
        Calls = calls;
        Stores = stores;
        Loads = loads;
        ReferencesType = referencesType;
        IsOfType = isOfType;
    }

    public IBinaryRelation<DotNetType, DotNetType, EmptyStruct> InheritsFrom { get; }

    public IBinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct> Overrides { get; }

    public IBinaryRelation<DotNetMethod, DotNetMethod, SyntaxNode> Calls { get; }

    public IBinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode> Stores { get; }

    public IBinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode> Loads { get; }

    public IBinaryRelation<DotNetElement, DotNetType, SyntaxNode> ReferencesType { get; }

    public IBinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> IsOfType { get; }

    public IEnumerator<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
    {
        yield return InheritsFrom.TransformData(_ => (SyntaxNode?) null);
        yield return Overrides.TransformData(_ => (SyntaxNode?) null);
        yield return Calls;
        yield return Stores;
        yield return Loads;
        yield return ReferencesType;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal class Builder
    {
        public BinaryRelation<DotNetType, DotNetType, EmptyStruct>.Builder InheritsFrom { get; } = new();

        public BinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct>.Builder Overrides { get; } = new();

        public BinaryRelation<DotNetMethod, DotNetMethod, SyntaxNode>.Builder Calls { get; } = new();

        public BinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode>.Builder Stores { get; } = new();

        public BinaryRelation<DotNetMethod, DotNetStorageTypeMember, SyntaxNode>.Builder Loads { get; } = new();

        public BinaryRelation<DotNetElement, DotNetType, SyntaxNode>.Builder ReferencesType { get; } = new();

        public BinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct>.Builder IsOfType { get; } = new();

        public DependencyRelations Build() =>
            new(
                InheritsFrom.Build(),
                Overrides.Build(),
                Calls.Build(),
                Stores.Build(),
                Loads.Build(),
                ReferencesType.Build(),
                IsOfType.Build());
    }
}
