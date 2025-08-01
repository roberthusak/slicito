namespace Slicito.Abstractions.Interaction;

public interface IWindowOpener
{
    Task OpenInNewWindowAsync(IController controller);
}
