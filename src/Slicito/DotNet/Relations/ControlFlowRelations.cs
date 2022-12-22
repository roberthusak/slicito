using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations(
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByUnconditionally,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfTrue,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfFalse,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithLeftOutInvocation)
{
    public IEnumerable<BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetIntraproceduralFlow()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithLeftOutInvocation;
    }

    public IEnumerable<BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetInterproceduralFlow()
    {
        throw new NotImplementedException();
    }
}
