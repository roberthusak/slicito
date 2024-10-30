using System.IO;
using System.Reflection;
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
using Slicito.DotNet;
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

    private DotNetFactProvider? _factProvider;

    public MainWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        _solutionPath = args[1];
        _controllersPath = args[2];
        _controllerType = args[3];
    }

    private async void Window_LoadedAsync(object sender, RoutedEventArgs e)
    {
        var controller = await LoadController();

        _contentControl.Content = new ToolPanel(controller);
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        var controller = await LoadController();

        _contentControl.Content = new ToolPanel(controller);
    }

    private async Task<IController> LoadController()
    {
        var assemblyBinary = await File.ReadAllBytesAsync(_controllersPath);

        var assembly = Assembly.Load(assemblyBinary);

        var type = assembly.GetTypes()
            .Where(t => typeof(IController).IsAssignableFrom(t) && t.Name == _controllerType)
            .Single();

        var constructor = type.GetConstructors().Single();

        var arguments = await LoadDependencies(constructor);

        var controller = Activator.CreateInstance(type, arguments)
            ?? throw new ApplicationException($"Unable to create an instance of the type {type.Name}.");

        return (IController) controller;
    }

    private async Task<object[]> LoadDependencies(ConstructorInfo constructor)
    {
        var dependencies = new List<object>();

        foreach (var parameter in constructor.GetParameters())
        {
            switch (parameter.ParameterType)
            {
                case var t when t == typeof(DotNetFactProvider):
                    dependencies.Add(await LoadFactProvider());
                    break;

                default:
                    throw new ApplicationException($"Unsupported parameter type {parameter.ParameterType.Name}.");
            }
        }

        return [.. dependencies];
    }

    private async Task<DotNetFactProvider> LoadFactProvider()
    {
        if (_factProvider == null)
        {
            var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);

            _factProvider = new DotNetFactProvider(solution);
        }

        return _factProvider;
    }
}
