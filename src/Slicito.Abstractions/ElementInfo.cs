namespace Slicito.Abstractions;

public record struct ElementInfo(ElementId Id, ElementType Type)
{
    public static implicit operator ElementId(ElementInfo info)
    {
        return info.Id;
    }
}
