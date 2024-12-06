using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.VisualStudio.Imaging;

using Slicito.Abstractions;
using Slicito.Wpf;

namespace Slicito.VisualStudio;
public class ControllerWindow : BaseToolWindow<ControllerWindow>
{
    private IController _controller;

    public override string GetTitle(int toolWindowId) => $"Slicito: {_controller?.GetType().Name} ({toolWindowId})";

    public override Type PaneType => typeof(Pane);

    public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
    {
        var package = Package as SlicitoPackage ?? throw new InvalidOperationException("Slicito package is not available from the main window.");
        _controller = package.ControllerRegistry.Get(toolWindowId);

        return Task.FromResult<FrameworkElement>(new ToolPanel(_controller));
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
