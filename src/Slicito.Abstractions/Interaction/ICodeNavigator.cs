namespace Slicito.Abstractions.Interaction;

public interface ICodeNavigator
{
    Task NavigateToAsync(CodeLocation codeLocation);
}
