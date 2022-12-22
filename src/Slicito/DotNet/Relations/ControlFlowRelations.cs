using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations(
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByUnconditionally,
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByIfTrue,
    BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?> IsSucceededByIfFalse)
{
    public IEnumerable<BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>> GetIntraproceduralFlow()
    {
        yield return IsSucceededByUnconditionally;
        yield return IsSucceededByIfTrue;
        yield return IsSucceededByIfFalse;
    }

    public IEnumerable<BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>> GetInterproceduralFlow()
    {
        throw new NotImplementedException();
    }
}
