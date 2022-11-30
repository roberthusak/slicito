using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;

namespace Slicito.Presentation;

public partial class Schema : IHtmlContent
{
    private readonly byte[] _contents;

    private readonly string _fileExtension;

    private Schema(byte[] contents, string fileExtension)
    {
        _contents = contents;
        _fileExtension = fileExtension;
    }

    public Stream GetContents() => new MemoryStream(_contents);

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        var contentsString = Encoding.UTF8.GetString(_contents);
        writer.Write(contentsString);
    }

    public async Task<SchemaReference> UploadToServerAsync(string name = "schema")
    {
        var uri = await ServerUtils.UploadFileAsync(name + _fileExtension, GetContents());

        return new SchemaReference(uri);
    }
}
