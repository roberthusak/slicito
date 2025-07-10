using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.Wpf;

namespace StandaloneGui;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly string _solutionPath;
    private readonly string _controllersPath;
    private readonly string _controllerType;
    private readonly string[] _additionalAssemblies;
    private readonly DependencyManager _dependencyManager;

    private AssemblyLoadContext? _assemblyLoadContext;

    public MainWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        _solutionPath = args[1];
        _controllersPath = args[2];
        _controllerType = args[3];
        _additionalAssemblies = [.. args.Skip(4)];

        _dependencyManager = new DependencyManager(_solutionPath, new TabOpener(this));
    }

    private async void Window_LoadedAsync(object sender, RoutedEventArgs e)
    {
        var controller = await LoadController();

        AddAndSelectTab("Start", controller);
    }

    private async void ReloadButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        var controller = await LoadController();

        _tabControl.Items.Clear();
        AddAndSelectTab("Start", controller);
    }

    private void AddAndSelectTab(string header, IController controller)
    {
        _tabControl.Items.Add(new TabItem
        {
            Header = header,
            Content = new ToolPanel(controller)
        });

        _tabControl.SelectedItem = _tabControl.Items[^1]; // Select the last added tab
    }

    private async Task<IController> LoadController()
    {
        _assemblyLoadContext?.Unload();
        _assemblyLoadContext = new AssemblyLoadContext("ControllerLoadContext", isCollectible: true);

        foreach (var assemblyPath in _additionalAssemblies)
        {
            LoadAssemblyWithoutLockingFile(_assemblyLoadContext, assemblyPath);
        }

        var controllersAssembly = LoadAssemblyWithoutLockingFile(_assemblyLoadContext, _controllersPath);

        var type = controllersAssembly.GetTypes()
            .Where(t => typeof(IController).IsAssignableFrom(t) && t.Name == _controllerType)
            .Single();

        var constructor = type.GetConstructors().Single();
        var arguments = await _dependencyManager.ResolveDependenciesAsync(constructor);

        var controller = Activator.CreateInstance(type, arguments)
            ?? throw new ApplicationException($"Unable to create an instance of the type {type.Name}.");

        return (IController)controller;
    }

    private static Assembly LoadAssemblyWithoutLockingFile(AssemblyLoadContext context, string assemblyPath)
    {
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
        
        return context.LoadFromStream(stream);
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (_tabControl.SelectedItem is TabItem selectedTab)
        {
            _tabControl.Items.Remove(selectedTab);
        }
    }

    private class TabOpener(MainWindow window) : IWindowOpener
    {
        public async Task OpenInNewWindowAsync(IController controller)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                window.AddAndSelectTab(controller.GetType().Name, controller);
            });
        }
    }
}
