
using Slicito.Abstractions;
using Slicito.Abstractions.Models;

namespace Slicito.Common.Controllers;

public class ModelDisplayer(IModel model) : IController
{
    public Task<IModel> InitAsync() => Task.FromResult(model);

    public Task<IModel?> ProcessCommandAsync(Command command) => Task.FromResult<IModel?>(model);
}