using Slicito.Abstractions.Interaction;
using Slicito.Common.Controllers;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.Common;

public static class ProgramAnalysisContextExtensions
{
    public static CallGraphExplorer CreateCallGraphExplorer(
        this IProgramAnalysisContext context,
        CallGraph callGraph,
        Action<CallGraphExplorer.Options>? configureOptions = null)
    {
        return new CallGraphExplorer(
            callGraph,
            context.ProgramTypes,
            context.FlowGraphProvider,
            context.GetService<ICodeNavigator>(),
            configureOptions);
    }
}
