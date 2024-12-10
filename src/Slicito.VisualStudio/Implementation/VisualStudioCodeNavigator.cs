using Microsoft.VisualStudio.Text;

using Slicito.Abstractions.Interaction;

namespace Slicito.VisualStudio.Implementation;

public class VisualStudioCodeNavigator : ICodeNavigator
{
    public async Task NavigateToAsync(CodeLocation codeLocation)
    {
        var view = await VS.Documents.OpenAsync(codeLocation.FullPath);

        var line = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(codeLocation.Line - 1);
        var point = line.Start.Add(codeLocation.Column);

        view.TextView.Caret.MoveTo(point);
        view.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point, 0));
    }
}
