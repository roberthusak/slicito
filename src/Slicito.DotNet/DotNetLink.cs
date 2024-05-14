using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetLink : ILink
{
    internal DotNetLink(DotNetElement source, DotNetElement target)
    {
        Source = source;
        Target = target;
    }

    public DotNetElement Source { get; }

    public DotNetElement Target { get; }

    IElement ILink.Source => Source;

    IElement ILink.Target => Source;
}
