using System.Runtime.InteropServices;

using Slicito.Abstractions.Interaction;

namespace StandaloneGui;

public class ExternalVisualStudioCodeNavigator : ICodeNavigator
{
    public Task NavigateToAsync(CodeLocation codeLocation)
    {
        try
        {
            // Inspired by https://stackoverflow.com/a/54869165/2105235
            // (other ways are cleaner but need proper handling of NuGet packages etc.)

            dynamic vs = Marshal2.GetActiveObject("VisualStudio.DTE");
            dynamic window = vs.ItemOperations.OpenFile(codeLocation.FullPath);
            window.Selection.MoveToLineAndOffset(codeLocation.Line, codeLocation.Column + 1);
        }
        catch (COMException e)
        {
            Console.Error.WriteLine(e);
        }

        return Task.CompletedTask;
    }
}
