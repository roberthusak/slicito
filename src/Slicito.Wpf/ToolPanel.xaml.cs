using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;

using SlicitoGraph = Slicito.Abstractions.Models.Graph;

namespace Slicito.Wpf;
/// <summary>
/// Interaction logic for ToolPanel.xaml
/// </summary>
public partial class ToolPanel : UserControl
{
    private readonly IController _controller;
    private readonly GraphViewer _graphViewer;

    public ToolPanel(IController controller)
    {
        InitializeComponent();

        _controller = controller;

        _graphViewer = new GraphViewer();
        _graphViewer.MouseDown += GraphViewer_MouseDownAsync;
    }

    private async void UserControl_LoadedAsync(object sender, RoutedEventArgs e)
    {
        _graphViewer.BindToPanel(_graphViewerPanel);

        _progressBar.IsIndeterminate = true;

        var model = await _controller.Init();

        ShowModel(model);

        _progressBar.IsIndeterminate = false;
    }

    private async void TreeView_MouseDoubleClickAsync(object sender, MouseButtonEventArgs e)
    {
        if ((sender as TreeView)?.SelectedItem is not TreeItem item)
        {
            return;
        }

        if (item.DoubleClickCommand is not null)
        {
            await ProcessCommand(item.DoubleClickCommand);
        }
    }

    private async void GraphViewer_MouseDownAsync(object? sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
    {
        switch (_graphViewer.ObjectUnderMouseCursor)
        {
            case IViewerNode viewerNode:
                if (viewerNode.Node.UserData is Abstractions.Models.Node { ClickCommand: Command nodeClickCommand })
                {
                    await ProcessCommand(nodeClickCommand);
                }
                break;

            case IViewerEdge viewerEdge:
                if (viewerEdge.Edge.UserData is Abstractions.Models.Edge { ClickCommand: Command edgeClickCommand })
                {
                    await ProcessCommand(edgeClickCommand);
                }
                break;
        }
    }

    private async Task ProcessCommand(Command command)
    {
        _progressBar.IsIndeterminate = true;

        var model = await _controller.ProcessCommand(command);

        if (model is not null)
        {
            ShowModel(model);
        }

        _progressBar.IsIndeterminate = false;
    }

    private void ShowModel(IModel model)
    {
        _treeView.Visibility = Visibility.Hidden;
        _graphViewerPanel.Visibility = Visibility.Hidden;

        switch (model)
        {
            case Tree tree:
                _treeView.DataContext = tree;
                _treeView.Visibility = Visibility.Visible;
                break;

            case SlicitoGraph graph:
                _graphViewer.Graph = SlicitoToMsaglGraphConverter.Convert(graph);
                _graphViewerPanel.Visibility = Visibility.Visible;
                break;
        }
    }
}
