using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.Common;
using Slicito.Common.Controllers;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.Repositories;

namespace Controllers;

public class ArchitectureAnalyzer : IController
{
    private static readonly string _optionsPathEnvironmentVariable = "SLICITO_ARCHITECTURE_ANALYZER_OPTIONS_PATH";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private StructureBrowser? _browser;

    public async Task<IModel> InitAsync()
    {
        var optionsPath = Environment.GetEnvironmentVariable(_optionsPathEnvironmentVariable)
            ?? throw new InvalidOperationException($"Environment variable {_optionsPathEnvironmentVariable} is not set.");
            
        using var jsonStream = File.OpenRead(optionsPath);

        var options = await JsonSerializer.DeserializeAsync<GitHubRepositoryDownloader.Options>(jsonStream, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize options from JSON file.");

        // Replace environment variable in base path
        options = options with { BasePath = options.BasePath.Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) };

        await new GitHubRepositoryDownloader().DownloadAsync(options);

        var solutionPaths = FindAllSolutionPaths(options);

        var typeSystem = new TypeSystem();
        var dotNetTypes = new DotNetTypes(typeSystem);
        var sliceManager = new SliceManager(typeSystem);
        var dotNetExtractor = new DotNetExtractor(dotNetTypes, sliceManager);

        var workspace = MSBuildWorkspace.Create();
        var solutions = new List<Solution>();
        foreach (var solutionPath in solutionPaths)
        {
            var solution = await workspace.OpenSolutionAsync(solutionPath);
            solutions.Add(solution);
        }

        var slice = dotNetExtractor.Extract([.. solutions]).Slice;

        _browser = new StructureBrowser(slice);

        return await _browser.InitAsync();
    }

    private static List<string> FindAllSolutionPaths(GitHubRepositoryDownloader.Options options)
    {
        var solutions = new List<string>();

        foreach (var organization in options.Organizations)
        {
            foreach (var repository in organization.Repositories)
            {
                var repositoryPath = Path.Combine(options.BasePath, organization.Name, repository.Name, "tags", repository.Tag);

                if (!Directory.Exists(repositoryPath))
                {
                    throw new InvalidOperationException($"Repository {repository.Name} (tag {repository.Tag}) of organization {organization.Name} cannot be found.");
                }

                // Find the .sln/.slnx file
                var solutionFiles = Directory.GetFiles(repositoryPath, "*.sln", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(repositoryPath, "*.slnx", SearchOption.AllDirectories));

                var solutionFile = solutionFiles.SingleOrDefault();
                
                if (solutionFile is not null)
                {
                    solutions.Add(solutionFile);
                }
            }
        }

        return solutions;
    }

    public async Task<IModel?> ProcessCommandAsync(Command command) => await _browser!.ProcessCommandAsync(command);
}
