using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Slicito.Server.Files;

namespace Slicito.Server.Controllers;

[ApiController]
[Route("files")]
public class FileRepositoryController : ControllerBase
{
    private readonly IFileRepository _repository;
    private readonly IContentTypeProvider _contentTypeProvider;

    public FileRepositoryController(IFileRepository repository, IContentTypeProvider contentTypeProvider)
    {
        _repository = repository;
        _contentTypeProvider = contentTypeProvider;
    }

    [HttpPut("{filename}")]
    public async Task PutFile([FromRoute] string filename)
    {
        await _repository.StoreFile(filename, Request.Body);
    }

    [HttpGet("{filename}")]
    public async Task<IActionResult> GetFile([FromRoute] string filename)
    {
        var contentStream = await _repository.LoadFile(filename);
        if (contentStream is null)
        {
            return NotFound();
        }

        if (_contentTypeProvider.TryGetContentType(filename, out var contentType))
        {
            HttpContext.Response.Headers.ContentType = contentType;
        }

        return Ok(contentStream);
    }
}
