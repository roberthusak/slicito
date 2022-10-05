namespace Slicito.Server.Files;

public class InMemoryFileRepository : IFileRepository
{
    Dictionary<string, byte[]> _fileContents = new();

    public Task<Stream?> LoadFile(string path)
    {
        if (!_fileContents.TryGetValue(path, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes));
    }

    public async Task StoreFile(string path, Stream content)
    {
        var ms = new MemoryStream();
        await content.CopyToAsync(ms);
        var bytes = ms.ToArray();

        _fileContents[path] = bytes;
    }
}
