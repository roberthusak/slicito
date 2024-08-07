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

using Slicito.Abstractions;

namespace Slicito.Wpf;
/// <summary>
/// Interaction logic for ToolPanel.xaml
/// </summary>
public partial class ToolPanel : UserControl
{
    private readonly IController _controller;

    public ToolPanel(IController controller)
    {
        InitializeComponent();

        _controller = controller;
    }

    private async void UserControl_LoadedAsync(object sender, RoutedEventArgs e)
    {
        _progressBar.IsIndeterminate = true;

        var model = await _controller.Init();

        _treeView.DataContext = model;

        _progressBar.IsIndeterminate = false;
    }
}
