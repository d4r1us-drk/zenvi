using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Services;

namespace Zenvi.Controllers;

[Authorize]
[Route("media")]
[ApiController]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName)
    {
        try
        {
            var (fileStream, contentType) = await mediaService.GetFileAsync(fileName);
            return File(fileStream, contentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}