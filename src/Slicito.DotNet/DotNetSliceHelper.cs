using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetSliceHelper
{
    public static ISlice GetProductionCodeSlice(ISlice originalSlice, DotNetSolutionContext solutionContext) =>
        new ProductionSlice(originalSlice, solutionContext);
}
