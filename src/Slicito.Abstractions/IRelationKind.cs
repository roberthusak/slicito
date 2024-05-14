using System.Collections.Immutable;

namespace Slicito.Abstractions;

public interface IRelationKind
{
    string Name { get; }

    ImmutableArray<IElementKind> SourceKinds { get; }

    ImmutableArray<IElementKind> TargetKinds { get; }
}
