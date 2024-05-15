using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal partial class FactQueryExecutor
{
    private struct KnownRequirements
    {
        public KnownRequirements(FactQuery query)
        {
            Solution = query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Solution);
            SolutionContains = query.RelationRequirements.FirstOrDefault(r => r.Kind == DotNetRelationKind.SolutionContains);
            Project = query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Project);
            ProjectContains = query.RelationRequirements.FirstOrDefault(r => r.Kind == DotNetRelationKind.ProjectContains);
            Namespace = query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Namespace);
            NamespaceContains = query.RelationRequirements.FirstOrDefault(r => r.Kind == DotNetRelationKind.NamespaceContains);
            Type = query.ElementRequirements.FirstOrDefault(r => r.Kind == DotNetElementKind.Type);
        }

        public FactQueryElementRequirement? Solution { get; }

        public FactQueryRelationRequirement? SolutionContains { get; }

        public FactQueryElementRequirement? Project { get; }

        public FactQueryRelationRequirement? ProjectContains { get; }

        public FactQueryElementRequirement? Namespace { get; }

        public FactQueryRelationRequirement? NamespaceContains { get; }

        public FactQueryElementRequirement? Type { get; }
    }
}
