using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.ProgramAnalysis;

public static class ProgramAnalysisContextExtensions
{
    public static CallGraph.Builder CreateCallGraphBuilder(this IProgramAnalysisContext context, ILazySlice slice) => new(slice, context.ProgramTypes);
}
