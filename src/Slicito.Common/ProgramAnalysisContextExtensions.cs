using Slicito.Abstractions;
using Slicito.Common.Controllers;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Interprocedural;

namespace Slicito.Common;

public static class ProgramAnalysisContextExtensions
{
    public static CallGraphExplorer CreateCallGraphExplorer(this IProgramAnalysisContext context, CallGraph callGraph)
    {
        return new CallGraphExplorer(callGraph, context.ProgramTypes);
    }
}
