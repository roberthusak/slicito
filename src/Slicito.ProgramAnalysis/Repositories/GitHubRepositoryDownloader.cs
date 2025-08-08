using Octokit;

using Slicito.Abstractions.Collections;

using System.IO.Compression;

namespace Slicito.ProgramAnalysis.Repositories;

public class GitHubRepositoryDownloader
{
    public record Repository(string Name, string? Tag);

    public record Organization(string Name, string PatEnvironmentVariable, ContentEquatableArray<Repository> Repositories);

    public record Options(string BasePath, ContentEquatableArray<Organization> Organizations, bool OverwriteIfExists = false);

    public async Task DownloadAsync(Options options)
    {
        foreach (var org in options.Organizations)
        {
            var pat = Environment.GetEnvironmentVariable(org.PatEnvironmentVariable);
            if (string.IsNullOrEmpty(pat))
            {
                throw new InvalidOperationException($"PAT environment variable '{org.PatEnvironmentVariable}' for organization '{org.Name}' is not set.");
            }

            var client = new GitHubClient(new ProductHeaderValue("Slicito"))
            {
                Credentials = new Credentials(pat)
            };

            foreach (var repo in org.Repositories)
            {
                var effectiveTag = repo.Tag ?? await GetLatestReleaseTagAsync(client, org.Name, repo.Name);

                var targetPath = Path.Combine(options.BasePath, org.Name, repo.Name, "tags", effectiveTag);
                
                if (Directory.Exists(targetPath) && Directory.EnumerateFileSystemEntries(targetPath).Any())
                {
                    if (options.OverwriteIfExists)
                    {
                        Directory.Delete(targetPath, recursive: true);
                    }
                    else
                    {
                        continue;
                    }
                }

                Directory.CreateDirectory(targetPath);

                try
                {
                        var zipBallContent = await client.Repository.Content.GetArchive(org.Name, repo.Name, ArchiveFormat.Zipball, effectiveTag);

                    ExtractZipBall(targetPath, zipBallContent);
                }
                catch (Exception ex)
                {
                        throw new InvalidOperationException($"Failed to download repository {org.Name}/{repo.Name} tag {effectiveTag}.", ex);
                }
            }
        }
    }

        private static async Task<string> GetLatestReleaseTagAsync(GitHubClient client, string owner, string repositoryName)
        {
            try
            {
                var release = await client.Repository.Release.GetLatest(owner, repositoryName);

                return release.TagName;
            }
            catch (NotFoundException ex)
            {
                throw new InvalidOperationException($"No releases found for repository {owner}/{repositoryName}. Specify a tag explicitly in configuration.", ex);
            }
        }

    private static void ExtractZipBall(string targetPath, byte[] zipBallContent)
    {
        using var zipStream = new MemoryStream(zipBallContent);
        using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        // Get the root folder name (first entry's full name)
        var rootFolder = zipArchive.Entries.First().FullName.Split('/')[0];

        // Extract only the contents of the root folder
        foreach (var entry in zipArchive.Entries)
        {
            if (entry.FullName.StartsWith(rootFolder + "/"))
            {
                var relativePath = entry.FullName[(rootFolder.Length + 1)..];
                if (string.IsNullOrEmpty(relativePath))
                {
                    continue;
                }

                var targetFile = Path.Combine(targetPath, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(targetFile, true);
                }
            }
        }
    }
}
