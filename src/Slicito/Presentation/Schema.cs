using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;

namespace Slicito.Presentation;

public partial class Schema : IHtmlContent
{
    private readonly byte[] _contents;

    private Schema(byte[] contents)
    {
        _contents = contents;
    }

    public Stream GetContents() => new MemoryStream(_contents);

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        var contentsString = Encoding.UTF8.GetString(_contents);
        writer.Write(contentsString);
    }

    public Task<string> UploadToServerAsync(string name = "schema") =>
        ServerUtils.UploadFileAsync(name + ".svg", GetContents());
}
