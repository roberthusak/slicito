using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Repositories;
using System.Text.Json;

namespace Controllers;

public class ArchitectureAnalyzer : IController
{
    private static readonly string _optionsPathEnvironmentVariable = "SLICITO_ARCHITECTURE_ANALYZER_OPTIONS_PATH";
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

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

        return new Tree([new("Completed", [])]);
    }

    public Task<IModel?> ProcessCommandAsync(Command command) => Task.FromResult((IModel?)null);
}
