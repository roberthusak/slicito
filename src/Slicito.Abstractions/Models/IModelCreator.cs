namespace Slicito.Abstractions.Models;

public interface IModelCreator<T>
{
    IModel CreateModel(T value);
}
