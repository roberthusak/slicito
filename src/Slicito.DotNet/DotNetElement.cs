using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetElement : IElement
{
    internal DotNetElement(DotNetElementKind kind, string id, string? name)
    {
        Kind = kind;
        Id = id;
        Name = name;
    }

    public DotNetElementKind Kind { get; }

    public string Id { get; }

    public string? Name { get; }

    IElementKind IElement.Kind => Kind;
}
