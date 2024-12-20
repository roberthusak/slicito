global using Community.VisualStudio.Toolkit;

global using Microsoft.VisualStudio.Shell;

global using System;

global using Task = System.Threading.Tasks.Task;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Common.Controllers;
using Slicito.Common.Extensibility;
using Slicito.VisualStudio.Implementation;

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Slicito.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideToolWindow(typeof(MainWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.SolutionExplorer)]
[ProvideToolWindow(typeof(ControllerWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.OutputWindow, MultiInstances = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuids.SlicitoString)]
public sealed class SlicitoPackage : ToolkitPackage
{
    internal VisualStudioWorkspace Workspace { get; private set; }

    internal VisualStudioSlicitoContext SlicitoContext { get; private set; }

    internal ControllerRegistry ControllerRegistry { get; } = new();

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await this.RegisterCommandsAsync();

        this.RegisterToolWindows();

        var componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

        Workspace = componentModel.GetService<VisualStudioWorkspace>();

        SlicitoContext = VisualStudioSlicitoContext.Create(Workspace);

        ScriptContext.SlicitoContext ??= SlicitoContext;
    }

    internal void OpenScript()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (!TryGetScriptPath(out var path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            CreateInitialScript(path);
        }

        if (VsShellUtilities.IsDocumentOpen(this, path, VSConstants.LOGVIEWID.Any_guid, out _, out _, out var windowFrame))
        {
            windowFrame.Show();
        }
        else
        {
            VsShellUtilities.OpenDocument(this, path, VSConstants.LOGVIEWID.Primary_guid, out _, out _, out _);
        }
    }

    private void CreateInitialScript(string path)
    {
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);

        var assemblies = new[]
        {
            typeof(Slicito.Abstractions.IController).Assembly,
            typeof(Slicito.ProgramAnalysis.IProgramTypes).Assembly,
            typeof(Slicito.Common.TypeSystem).Assembly,
            typeof(Slicito.DotNet.DotNetExtractor).Assembly,
        };

        foreach (var assembly in assemblies)
        {
            writer.WriteLine($"#r \"{assembly.Location}\"");
        }

        writer.WriteLine();
        writer.WriteLine("using Slicito.Common;");
        writer.WriteLine("using Slicito.DotNet;");
        writer.WriteLine("using Slicito.ProgramAnalysis;");
        writer.WriteLine();
        writer.WriteLine("using static Slicito.Common.Extensibility.ScriptContext;");
        writer.WriteLine();
    }

    internal async Task RunScriptAsync()
    {
        if (!TryGetScriptPath(out var path))
        {
            return;
        }

        var result = await ScriptRunner.RunScriptAsync(path);

        if (result is IController controller)
        {
            await CreateToolWindowAsync(controller);
        }
        else if (result is IModel model)
        {
            await CreateToolWindowAsync(new ModelDisplayer(model));
        }
        else if (result is object obj)
        {
            var textModel = new Tree([new(obj.ToString(), [])]);
            await CreateToolWindowAsync(new ModelDisplayer(textModel));
        }
    }

    private bool TryGetScriptPath(out string path)
    {
        if (Workspace.CurrentSolution?.FilePath is not string solutionPath)
        {
            path = null;
            return false;
        }

        var solutionDir = Path.GetDirectoryName(solutionPath);

        path = Path.Combine(solutionDir, "slicito.csx");
        return true;
    }

    internal async Task CreateToolWindowAsync(IController controller)
    {
        var id = ControllerRegistry.Register(controller);

        var window = await FindToolWindowAsync(typeof(ControllerWindow.Pane), id, true, DisposalToken);
        if (window is null || window.Frame is null)
        {
            throw new InvalidOperationException("Cannot create a tool window.");
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

        var windowFrame = (IVsWindowFrame) window.Frame;
        Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }
}
