using Slicito.Abstractions;
using Slicito.ProgramAnalysis;

namespace Slicito.DotNet;

public static class ProgramAnalysisContextExtensions
{
    public static async Task<ElementInfo> FindSingleMethodAsync(this IProgramAnalysisContext context, ILazySlice slice, string nameSuffix)
    {
        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(slice, (DotNetTypes)context.ProgramTypes);
        
        return methods.Single(m => m.DisplayName.EndsWith(nameSuffix)).Method;
    }
}
