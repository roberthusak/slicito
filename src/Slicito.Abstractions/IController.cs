using Slicito.Abstractions.Models;

namespace Slicito.Abstractions;

public interface IController
{
    Task<IModel> Init();
}