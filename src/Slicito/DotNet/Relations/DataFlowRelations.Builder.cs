using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record DataFlowRelations
{
    public class Builder
    {
        public Builder(DependencyRelations dependencyRelations)
        {
            DependencyRelations = dependencyRelations;
        }

        public DependencyRelations DependencyRelations { get; }

        public Relation<DotNetVariable, DotNetOperation, SyntaxNode>.Builder VariableIsReadBy { get; } = new();

        public Relation<DotNetOperation, DotNetVariable, SyntaxNode>.Builder WritesToVariable { get; } = new();

        public Relation<DotNetOperation, DotNetOperation, SyntaxNode>.Builder ResultIsCopiedTo { get; } = new();

        public Relation<DotNetOperation, DotNetOperation, SyntaxNode>.Builder ResultIsReadBy { get; } = new();

        public Relation<DotNetOperation, DotNetParameter, SyntaxNode>.Builder IsPassedAs { get; } = new();

        public DataFlowRelations Build() =>
            new(
                VariableIsReadBy.Build(),
                WritesToVariable.Build(),
                ResultIsCopiedTo.Build(),
                ResultIsReadBy.Build(),
                IsPassedAs.Build(),
                DependencyRelations.StoresTo,
                DependencyRelations.LoadsFrom.Invert());
    }
}
