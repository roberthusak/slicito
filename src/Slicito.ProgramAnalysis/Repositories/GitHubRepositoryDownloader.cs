using Octokit;
using System.IO.Compression;

namespace Slicito.ProgramAnalysis.Repositories;

public class GitHubRepositoryDownloader
{
    public record Repository(string Name, string Tag);

    public record Organization(string Name, string PatEnvironmentVariable, List<Repository> Repositories);

    public record Options(string BasePath, List<Organization> Organizations, bool DeleteIfExists = false);

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
                var targetPath = Path.Combine(options.BasePath, org.Name, repo.Name, "tags", repo.Tag);
                
                if (Directory.Exists(targetPath) && Directory.EnumerateFileSystemEntries(targetPath).Any())
                {
                    if (options.DeleteIfExists)
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
                    var zipBallContent = await client.Repository.Content.GetArchive(org.Name, repo.Name, ArchiveFormat.Zipball, repo.Tag);

                    ExtractZipBall(targetPath, zipBallContent);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to download repository {org.Name}/{repo.Name} tag {repo.Tag}.", ex);
                }
            }
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

