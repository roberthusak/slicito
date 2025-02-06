using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Controllers;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.DotNet;
using Slicito.DotNet.AspNetCore;
namespace Slicito.VisualStudio;

public partial class MainWindowControl : UserControl
{
    private readonly SlicitoPackage _package;

    private readonly List<Tuple<string, Func<IController>>> _controllerFactories;

    public MainWindowControl(SlicitoPackage package)
    {
        InitializeComponent();
        _package = package;

        _controllerFactories =
        [
            new("Structure Browser", () => new SampleStructureBrowser(
                _package.SlicitoContext.TypeSystem,
                (DotNetSolutionContext) _package.SlicitoContext.FlowGraphProvider,
                _package.SlicitoContext.WholeSlice)),

            new("API Endpoint Catalog", () => new ApiEndpointCatalog(
                _package.SlicitoContext.WholeSlice,
                (DotNetSolutionContext) _package.SlicitoContext.FlowGraphProvider,
                _package.SlicitoContext.ProgramTypes,
                _package.SlicitoContext.GetService<ICodeNavigator>())),
        ];

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
        var controller = factory();

        await _package.CreateToolWindowAsync(controller);
    }

    private void OnOpenScript(object sender, RoutedEventArgs e) => _package.OpenScript();

    private void OnRunScript(object sender, RoutedEventArgs e) => _package.JoinableTaskFactory.RunAsync(_package.RunScriptAsync);
}
