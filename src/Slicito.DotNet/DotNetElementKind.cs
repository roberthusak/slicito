using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetElementKind : IElementKind
{
    public string Name { get; }

    private DotNetElementKind(string name)
    {
        Name = name;
    }

    public static DotNetElementKind Solution { get; } = new DotNetElementKind(DotNetElementKindNames.Solution);

    public static DotNetElementKind Project { get; } = new DotNetElementKind(DotNetElementKindNames.Project);

    public static DotNetElementKind Namespace { get; } = new DotNetElementKind(DotNetElementKindNames.Namespace);

    public static DotNetElementKind Type { get; } = new DotNetElementKind(DotNetElementKindNames.Type);

    public static DotNetElementKind Method { get; } = new DotNetElementKind(DotNetElementKindNames.Method);

    public static DotNetElementKind Field { get; } = new DotNetElementKind(DotNetElementKindNames.Field);

    public static DotNetElementKind Operation { get; } = new DotNetElementKind(DotNetElementKindNames.Operation);
}
