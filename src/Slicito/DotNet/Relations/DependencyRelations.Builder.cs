using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class DependencyRelations
{
    internal class Builder
    {
        public BinaryRelation<DotNetType, DotNetType, EmptyStruct>.Builder InheritsFrom { get; } = new();

        public BinaryRelation<DotNetMethod, DotNetMethod, EmptyStruct>.Builder Overrides { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetMethod, SyntaxNode>.Builder Calls { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode>.Builder StoresTo { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode>.Builder LoadsFrom { get; } = new();

        public BinaryRelation<DotNetElement, DotNetType, SyntaxNode>.Builder ReferencesType { get; } = new();

        public BinaryRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct>.Builder IsOfType { get; } = new();

        public DependencyRelations Build() =>
            new(
                InheritsFrom.Build(),
                Overrides.Build(),
                Calls.Build(),
                StoresTo.Build(),
                LoadsFrom.Build(),
                ReferencesType.Build(),
                IsOfType.Build());
    }
}
