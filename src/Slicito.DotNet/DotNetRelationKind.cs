using System.Collections.Immutable;

using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetRelationKind : IRelationKind
{
    public string Name { get; }

    public ImmutableArray<IElementKind> SourceKinds { get; }

    public ImmutableArray<IElementKind> TargetKinds { get; }

    private DotNetRelationKind(string name, ImmutableArray<IElementKind> sourceKinds, ImmutableArray<IElementKind> targetKinds)
    {
        Name = name;
        SourceKinds = sourceKinds;
        TargetKinds = targetKinds;
    }

    public static DotNetRelationKind SolutionContains { get; } =
        new DotNetRelationKind("SolutionContains", [DotNetElementKind.Solution], [DotNetElementKind.Project]);

    public static DotNetRelationKind ProjectContains { get; } =
        new DotNetRelationKind("ProjectContains", [DotNetElementKind.Project], [DotNetElementKind.Namespace]);

    public static DotNetRelationKind NamespaceContains { get; } =
        new DotNetRelationKind("NamespaceContains", [DotNetElementKind.Namespace], [DotNetElementKind.Type]);

    public static DotNetRelationKind TypeContains { get; } =
        new DotNetRelationKind("TypeContains", [DotNetElementKind.Type], [DotNetElementKind.Type, DotNetElementKind.Method, DotNetElementKind.Field]);

    public static DotNetRelationKind MethodContains { get; } =
        new DotNetRelationKind("MethodContains", [DotNetElementKind.Method], [DotNetElementKind.Operation]);
}
