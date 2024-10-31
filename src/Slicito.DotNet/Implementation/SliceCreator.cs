using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly Solution _solution;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private SliceCreator(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        _solution = solution;
        _types = types;
        _sliceManager = sliceManager;
    }

    public static ILazySlice CreateSlice(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        var creator = new SliceCreator(solution, types, sliceManager);

        return creator.CreateSlice();
    }

    private ILazySlice CreateSlice()
    {
        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Project, LoadProjectElementsAsync)
            .AddElementAttribute(_types.Project, "Name", LoadProjectNameAsync)
            .BuildLazy();
    }

    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadProjectElementsAsync()
    {
        var elements = _solution.Projects
            .Select(project => new ISliceBuilder.PartialElementInfo(ElementIdProvider.GetElementId(project)));

        return new(elements);
    }

    private ValueTask<string> LoadProjectNameAsync(ElementId elementId)
    {
        var name = Path.GetFileName(elementId.Value);

        return new(name);
    }
}
