using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record ControlFlowRelations
{
    public class Builder
    {
        public Builder(DependencyRelations dependencyRelations)
        {
            DependencyRelations = dependencyRelations;
        }

        public DependencyRelations DependencyRelations { get; }

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByUnconditionally { get; } = new();

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByIfTrue { get; } = new();

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByIfFalse { get; } = new();

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByWithLeftOutInvocation { get; } = new();

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByWithInvocation { get; } = new();

        public Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder IsSucceededByWithReturn { get; } = new();

        public ControlFlowRelations Build() =>
            new(
                IsSucceededByUnconditionally.Build(),
                IsSucceededByIfTrue.Build(),
                IsSucceededByIfFalse.Build(),
                IsSucceededByWithLeftOutInvocation.Build(),
                IsSucceededByWithInvocation.Build(),
                IsSucceededByWithReturn.Build());
    }
}
