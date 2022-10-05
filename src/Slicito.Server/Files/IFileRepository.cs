namespace Slicito.Server.Files;

public interface IFileRepository
{
    Task<Stream?> LoadFile(string path);

    Task StoreFile(string path, Stream content);
}
