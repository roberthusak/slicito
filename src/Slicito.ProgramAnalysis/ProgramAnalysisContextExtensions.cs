using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Interprocedural;
using Slicito.ProgramAnalysis.Reachability;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.ProgramAnalysis;

public static class ProgramAnalysisContextExtensions
{
    public static CallGraph.Builder CreateCallGraphBuilder(this IProgramAnalysisContext context, ILazySlice slice) => new(slice, context.ProgramTypes);

    public static ReachabilityAnalysis.Builder CreateReachabilityBuilder(this IProgramAnalysisContext context) =>
        new(context.FlowGraphProvider, context.GetService<ISolverFactory>());
}
