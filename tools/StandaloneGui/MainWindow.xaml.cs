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

    public MainWindow()
    {
        InitializeComponent();

        var args = Environment.GetCommandLineArgs();
        _solutionPath = args[1];
        _controllersPath = args[2];
    }

    private async void Window_LoadedAsync(object sender, RoutedEventArgs e)
    {
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);

        var provider = new DotNetFactProvider(solution);

        var controller = LoadController(provider);

        _contentPanel.Children.Add(new ToolPanel(controller));
    }

    private IController LoadController(DotNetFactProvider provider)
    {
        var assembly = Assembly.LoadFrom(_controllersPath);

        var type = assembly.GetTypes().Single(t => typeof(IController).IsAssignableFrom(t));

        var controller = Activator.CreateInstance(type, provider)
            ?? throw new ApplicationException($"Unable to create an instance of the type {type.Name}.");

        return (IController)controller;
    }
}
