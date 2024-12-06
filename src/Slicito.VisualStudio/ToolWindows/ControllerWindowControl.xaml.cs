using System.Windows;
using System.Windows.Controls;

namespace Slicito.VisualStudio;
public partial class ControllerWindowControl : UserControl
{
    public ControllerWindowControl()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, RoutedEventArgs e)
    {
        VS.MessageBox.Show("ControllerWindowControl", "Button clicked");
    }
}
