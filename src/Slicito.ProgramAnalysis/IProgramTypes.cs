using Slicito.Abstractions;

namespace Slicito.ProgramAnalysis;

public interface IProgramTypes
{
    LinkType Contains { get; }
    LinkType Calls { get; }
        
    ElementType Procedure { get; }

    ElementType Call { get; }

    bool HasName(ElementType elementType);
}
