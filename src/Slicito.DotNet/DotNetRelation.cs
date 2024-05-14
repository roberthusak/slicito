using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetRelation : IRelation
{
    internal DotNetRelation(DotNetRelationKind kind, IEnumerable<DotNetLink> links)
    {
        Kind = kind;
        Links = links;
    }

    public DotNetRelationKind Kind { get; }

    public IEnumerable<DotNetLink> Links { get; }

    IRelationKind IRelation.Kind => Kind;

    IEnumerable<ILink> IRelation.Links => Links;
}
