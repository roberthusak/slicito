using Slicito.Abstractions.Models;

namespace Slicito.Abstractions;

public interface IController
{
    Task<IModel> InitAsync();

    Task<IModel?> ProcessCommandAsync(Command command);
}
