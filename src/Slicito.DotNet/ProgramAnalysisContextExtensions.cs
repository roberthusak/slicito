using Slicito.Abstractions;
using Slicito.ProgramAnalysis;

namespace Slicito.DotNet;

public static class ProgramAnalysisContextExtensions
{
    public static ILazySlice GetProductionCodeSlice(this IProgramAnalysisContext context) =>
        DotNetSliceHelper.GetProductionCodeSlice(context.WholeSlice, (DotNetSolutionContext) context.FlowGraphProvider);

    public static async Task<ElementInfo> FindSingleMethodAsync(this IProgramAnalysisContext context, ILazySlice slice, string nameSuffix) =>
        await DotNetMethodHelper.FindSingleMethodAsync(slice, (DotNetTypes)context.ProgramTypes, nameSuffix);
}
