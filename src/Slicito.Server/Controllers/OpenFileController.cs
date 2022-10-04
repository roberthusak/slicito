using Microsoft.AspNetCore.Mvc;

namespace Slicito.Server.Controllers;

[ApiController]
[Route("open")]
public class OpenFileController : ControllerBase
{
    [HttpGet]
    public void OpenFile(
        [FromQuery] string path,
        [FromQuery] int line,
        [FromQuery] int offset)
    {
        // Inspired by https://stackoverflow.com/a/54869165/2105235
        // (other ways are cleaner but need proper handling of NuGet packages etc.)

        dynamic vs = Marshal2.GetActiveObject("VisualStudio.DTE");
        dynamic window = vs.ItemOperations.OpenFile(path);
        window.Selection.MoveToLineAndOffset(line, offset);
    }
}