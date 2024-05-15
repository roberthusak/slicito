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
        }

        public FactQueryElementRequirement? Solution { get; }

        public FactQueryRelationRequirement? SolutionContains { get; }

        public FactQueryElementRequirement? Project { get; }
    }
}
