using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations(
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByUnconditionally,
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByIfTrue,
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByIfFalse,
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByWithLeftOutInvocation)
{
    public IEnumerable<BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>> GetIntraproceduralFlow()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
        yield return IsSucceededByWithLeftOutInvocation;
    }

    public IEnumerable<BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>> GetInterproceduralFlow()
    {
        throw new NotImplementedException();
    }
}
