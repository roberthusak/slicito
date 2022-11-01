using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Slicito.DotNet;

internal static class RoslynUtils
{
    static RoslynUtils()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var sdks = MSBuildLocator.QueryVisualStudioInstances();

            var net6_0_100sdk = sdks.FirstOrDefault(sdk =>
                sdk.Version.Major == 6
                && sdk.Version.Minor == 0
                && sdk.Version.Build == 100);
            if (net6_0_100sdk != null)
            {
                // Currently, .NET 6.0.100 SDK is needed (as explained in https://github.com/dotnet/interactive/issues/1985)
                MSBuildLocator.RegisterInstance(net6_0_100sdk);
            }
            else if (sdks.Any())
            {
                // Check if there isn't at least something like 6.0.109 present
                var bestSdk =
                    sdks.FirstOrDefault(sdk => sdk.Version.Major == 6 && sdk.Version.Minor == 0 && sdk.Version.Build > 100 && sdk.Version.Build < 200)
                    ?? sdks.First();

                Console.WriteLine($"Warning: The .NET 6.0.100 SDK not found, registering '{bestSdk.MSBuildPath}' instead.");
                MSBuildLocator.RegisterInstance(bestSdk);
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
