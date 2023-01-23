using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations(
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByUnconditionally,
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfTrue,
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByIfFalse,
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithLeftOutInvocation,
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithInvocation,
    Relation<DotNetElement, DotNetElement, SyntaxNode?> IsSucceededByWithReturn)
    : IEnumerable<Relation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    public IEnumerable<Relation<DotNetElement, DotNetElement, SyntaxNode?>> GetIntraproceduralFlow()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithLeftOutInvocation;
    }

    public IEnumerable<Relation<DotNetElement, DotNetElement, SyntaxNode?>> GetInterproceduralFlow()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithInvocation;
        yield return IsSucceededByWithReturn;
    }

    public IEnumerator<Relation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithLeftOutInvocation;
        yield return IsSucceededByWithInvocation;
        yield return IsSucceededByWithReturn;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
