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
        progressBar.IsIndeterminate = true;

        var args = Environment.GetCommandLineArgs();
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(args[1]);

        var provider = new DotNetFactProvider(solution);

        var query = new FactQuery(
            [
                new FactQueryElementRequirement(DotNetElementKind.Solution, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Project, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Namespace, IncludeChildless: false),
                new FactQueryElementRequirement(DotNetElementKind.Type, IncludeChildless: true),
            ],
            [
                new FactQueryRelationRequirement(DotNetRelationKind.SolutionContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.ProjectContains, IncludeChildless: false),
                new FactQueryRelationRequirement(DotNetRelationKind.NamespaceContains, IncludeChildless: false)
            ]);

        var result = await provider.QueryAsync(query);

        foreach (var element in result.Elements)
        {
            treeView.Items.Add(new TreeViewItem { Header = element.Id });
        }

        progressBar.IsIndeterminate = false;
    }
}
