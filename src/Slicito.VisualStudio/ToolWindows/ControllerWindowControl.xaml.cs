using System.Windows;
using System.Windows.Controls;

using Slicito.Abstractions;
using Slicito.Wpf;

namespace Slicito.VisualStudio;
public partial class ControllerWindowControl : UserControl
{
    public ControllerWindowControl(IController controller)
    {
        InitializeComponent();

        _contentControl.Content = new ToolPanel(controller);
    }
}
