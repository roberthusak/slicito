using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Slicito;

public static class RoslynUtils
{
    static RoslynUtils()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var sdks = MSBuildLocator.QueryVisualStudioInstances();

            var firstNet5sdk = sdks.FirstOrDefault(sdk => sdk.Version.Major == 5);
            if (firstNet5sdk != null)
            {
                // Currently, .NET 5 SDK is needed (as explained in https://github.com/dotnet/interactive/issues/1985)
                MSBuildLocator.RegisterInstance(firstNet5sdk);
            }
            else if (sdks.Any())
            {
                var bestSdk = sdks.First();
                Console.WriteLine($"Warning: No .NET 5 SDK found, registering '{bestSdk.MSBuildPath}' instead.");
            }
            else
            {
                Console.WriteLine($"Warning: No .NET SDK found, loading Roslyn projects may not work correctly.");
            }
        }
    }

    public static Task<Project> OpenProjectAsync(string path)
    {
        using var workspace = MSBuildWorkspace.Create();

        return workspace.OpenProjectAsync(path);
    }
}
