using Microsoft.CodeAnalysis;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations
{
    internal class Builder
    {
        public Builder(DependencyRelations dependencyRelations)
        {
            DependencyRelations = dependencyRelations;
        }

        public DependencyRelations DependencyRelations { get; }

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByUnconditionally { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByIfTrue { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByIfFalse { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByWithLeftOutInvocation { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByWithDynamicDispatch { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder IsSucceededByWithReturn { get; } = new();

        public ControlFlowRelations Build() =>
            new(
                IsSucceededByUnconditionally.Build(),
                IsSucceededByIfTrue.Build(),
                IsSucceededByIfFalse.Build(),
                IsSucceededByWithLeftOutInvocation.Build());
    }
}
