using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Abstractions.Queries;
using Slicito.Common.Controllers;

namespace Controllers;

public class SampleStructureBrowser(ITypeSystem typeSystem, ISlice? slice = null) : IController
{
    private readonly StructureBrowser _browser = new(slice ?? SliceHelper.CreateSampleSlice(typeSystem));

    public Task<IModel> InitAsync() => _browser.InitAsync();

    public async Task<IModel?> ProcessCommandAsync(Command command) => await _browser.ProcessCommandAsync(command);
}
