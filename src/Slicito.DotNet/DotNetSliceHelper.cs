using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetSliceHelper
{
    public static ILazySlice GetProductionCodeSlice(ILazySlice originalSlice, DotNetSolutionContext solutionContext) =>
        new ProductionSlice(originalSlice, solutionContext);
}
