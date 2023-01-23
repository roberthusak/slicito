using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record DependencyRelations
{
    public class Builder
    {
        public Relation<DotNetType, DotNetType, EmptyStruct>.Builder InheritsFrom { get; } = new();

        public Relation<DotNetMethod, DotNetMethod, EmptyStruct>.Builder Overrides { get; } = new();

        public Relation<DotNetOperation, DotNetMethod, SyntaxNode>.Builder Calls { get; } = new();

        public Relation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode>.Builder StoresTo { get; } = new();

        public Relation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode>.Builder LoadsFrom { get; } = new();

        public Relation<DotNetElement, DotNetType, SyntaxNode>.Builder ReferencesType { get; } = new();

        public Relation<DotNetStorageTypeMember, DotNetType, EmptyStruct>.Builder IsOfType { get; } = new();

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
