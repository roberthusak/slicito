namespace Slicito.VisualStudio;

[Command(PackageIds.SlicitoMainWindowCommand)]
internal sealed class SlicitoMainWindowCommand : BaseCommand<SlicitoMainWindowCommand>
{
    protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        return MainWindow.ShowAsync();
    }
}
