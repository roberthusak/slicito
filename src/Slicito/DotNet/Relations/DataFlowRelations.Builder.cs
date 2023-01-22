using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial class DataFlowRelations
{
    internal class Builder
    {
        public Builder(DependencyRelations dependencyRelations)
        {
            DependencyRelations = dependencyRelations;
        }

        public DependencyRelations DependencyRelations { get; }

        public BinaryRelation<DotNetVariable, DotNetOperation, SyntaxNode>.Builder VariableIsReadBy { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetVariable, SyntaxNode>.Builder WritesToVariable { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode>.Builder ResultIsCopiedTo { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode>.Builder ResultIsReadBy { get; } = new();

        public BinaryRelation<DotNetOperation, DotNetParameter, SyntaxNode>.Builder IsPassedAs { get; } = new();

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
