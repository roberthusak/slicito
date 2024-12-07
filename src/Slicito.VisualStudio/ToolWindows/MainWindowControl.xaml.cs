using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Controllers;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Slicito.Abstractions;
using Slicito.Common;
namespace Slicito.VisualStudio;

public partial class MainWindowControl : UserControl
{
    private readonly SlicitoPackage _package;

    private readonly List<Tuple<string, Func<IController>>> _controllerFactories =
    [
        new("Sample Structure Browser", () => new SampleStructureBrowser(new TypeSystem())),
    ];

    public MainWindowControl(SlicitoPackage package)
    {
        InitializeComponent();
        _package = package;

        _controllersComboBox.ItemsSource = _controllerFactories;
    }

    private void OnCreateWindow(object sender, RoutedEventArgs e) => _package.JoinableTaskFactory.RunAsync(OnCreateWindowAsync);

    private async Task OnCreateWindowAsync()
    {
        if (_controllersComboBox.SelectedIndex < 0)
        {
            return;
        }

        var (name, factory) = _controllerFactories[_controllersComboBox.SelectedIndex];
        var id = _package.ControllerRegistry.Register(factory());

        var window = await _package.FindToolWindowAsync(typeof(ControllerWindow.Pane), id, true, _package.DisposalToken);
        if (window is null || window.Frame is null)
        {
            throw new InvalidOperationException("Cannot create a tool window.");
        }

        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

        var windowFrame = (IVsWindowFrame) window.Frame;
        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }

    private void OnOpenScript(object sender, RoutedEventArgs e) => throw new NotImplementedException();

    private void OnRunScript(object sender, RoutedEventArgs e) => throw new NotImplementedException();
}
