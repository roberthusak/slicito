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
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_LoadedAsync(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(args[1]);

        var provider = new DotNetFactProvider(solution);

        var controller = new DotNetTreeController(provider);

        _contentPanel.Children.Add(new ToolPanel(controller));
    }
}
