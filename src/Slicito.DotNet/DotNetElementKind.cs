using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetElementKind : IElementKind
{
    public string Name { get; }

    private DotNetElementKind(string name)
    {
        Name = name;
    }

    public static DotNetElementKind Solution { get; } = new DotNetElementKind("Solution");

    public static DotNetElementKind Project { get; } = new DotNetElementKind("Project");

    public static DotNetElementKind Namespace { get; } = new DotNetElementKind("Namespace");

    public static DotNetElementKind Type { get; } = new DotNetElementKind("Type");

    public static DotNetElementKind Method { get; } = new DotNetElementKind("Method");

    public static DotNetElementKind Field { get; } = new DotNetElementKind("Field");

    public static DotNetElementKind Operation { get; } = new DotNetElementKind("Operation");
}
