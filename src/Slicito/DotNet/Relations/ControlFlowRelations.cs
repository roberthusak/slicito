using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations(
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByUnconditionally,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfTrue,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfFalse,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithLeftOutInvocation,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithInvocation,
    BinaryRelation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithReturn)
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
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithInvocation;
        yield return IsSucceededByWithReturn;
    }
}
