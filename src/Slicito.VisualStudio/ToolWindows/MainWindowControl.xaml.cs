using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Controllers;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Slicito.Common;
namespace Slicito.VisualStudio;

public partial class MainWindowControl : UserControl
{
    private readonly SlicitoPackage _package;

    public MainWindowControl(SlicitoPackage package)
    {
        InitializeComponent();
        _package = package;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e) => _package.JoinableTaskFactory.RunAsync(OnButtonClickAsync);

    private async Task OnButtonClickAsync()
    {
        var id = _package.ControllerRegistry.Register(new SampleStructureBrowser(new TypeSystem()));

        var window = await _package.FindToolWindowAsync(typeof(ControllerWindow.Pane), id, true, _package.DisposalToken);
        if (window is null || window.Frame is null)
        {
            throw new InvalidOperationException("Cannot create a tool window.");
        }

        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

        var windowFrame = (IVsWindowFrame) window.Frame;
        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }
}
