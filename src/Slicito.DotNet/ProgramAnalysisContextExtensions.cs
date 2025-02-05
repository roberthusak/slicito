using Slicito.Abstractions;
using Slicito.ProgramAnalysis;

namespace Slicito.DotNet;

public static class ProgramAnalysisContextExtensions
{
    public static async Task<ElementInfo> FindSingleMethodAsync(this IProgramAnalysisContext context, ILazySlice slice, string nameSuffix)
    {
        return await DotNetMethodHelper.FindSingleMethodAsync(slice, (DotNetTypes)context.ProgramTypes, nameSuffix);
    }
}
