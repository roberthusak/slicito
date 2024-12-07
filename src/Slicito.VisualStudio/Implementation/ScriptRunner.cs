using System.IO;
using System.Threading.Tasks;

using Dotnet.Script.Core;
using Dotnet.Script.Core.Commands;

namespace Slicito.VisualStudio.Implementation;

internal class ScriptRunner
{
    public static async Task<object> RunScriptAsync(string path)
    {
        var cwd = Path.GetFullPath(Path.GetDirectoryName(path));

        var script = File.ReadAllText(path);

        var options = new ExecuteCodeCommandOptions(script, cwd, [], Microsoft.CodeAnalysis.OptimizationLevel.Release, true, []);

        var command = new ExecuteCodeCommand(ScriptConsole.Default, _ => (_, _, _) => Nothing());

        return await command.Execute<object>(options);
    }

    private static void Nothing()
    {
    }
}
