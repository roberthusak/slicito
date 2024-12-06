using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
namespace Slicito.VisualStudio;

public partial class MainWindowControl : UserControl
{
    private readonly ToolkitPackage _package;

    public MainWindowControl(ToolkitPackage package)
    {
        InitializeComponent();
        _package = package;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e) => _package.JoinableTaskFactory.RunAsync(OnButtonClickAsync);

    private async Task OnButtonClickAsync()
    {
        for (var i = 0; i < 3; i++)
        {
            var window = await _package.FindToolWindowAsync(typeof(ControllerWindow.Pane), i, false, _package.DisposalToken);

            if (window == null)
            {
                // Create the window with the first free ID.
                window = await _package.FindToolWindowAsync(typeof(ControllerWindow.Pane), i, true, _package.DisposalToken);
                if (window is null || window.Frame is null)
                {
                    throw new InvalidOperationException("Cannot create a tool window.");
                }

                await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

                var windowFrame = (IVsWindowFrame) window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                break;
            }
        }
    }
}
