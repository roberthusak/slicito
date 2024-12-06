using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Slicito.VisualStudio;

public class MainWindow : BaseToolWindow<MainWindow>
{
    public override string GetTitle(int toolWindowId) => "Slicito";

    public override Type PaneType => typeof(Pane);

    public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
    {
        return Task.FromResult<FrameworkElement>(new MainWindowControl());
    }

    [Guid("681e5668-1e63-46cf-881d-1ca8d6fc2e51")]
    internal class Pane : ToolkitToolWindowPane
    {
        public Pane()
        {
            BitmapImageMoniker = KnownMonikers.ToolWindow;
        }
    }
}
