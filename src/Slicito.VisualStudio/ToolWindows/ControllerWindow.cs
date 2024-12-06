using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.VisualStudio.Imaging;

namespace Slicito.VisualStudio;
public class ControllerWindow : BaseToolWindow<ControllerWindow>
{
    public override string GetTitle(int toolWindowId) => $"Slicito: Controller {toolWindowId}";

    public override Type PaneType => typeof(Pane);

    public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
    {
        return Task.FromResult<FrameworkElement>(new ControllerWindowControl());
    }

    [Guid("996d5047-928e-4eab-af50-dd7343f6531a")]
    internal class Pane : ToolkitToolWindowPane
    {
        public Pane()
        {
            BitmapImageMoniker = KnownMonikers.ToolWindow;
        }
    }
}
