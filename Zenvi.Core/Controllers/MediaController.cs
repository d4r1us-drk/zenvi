using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Core.Services;

namespace Zenvi.Core.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        try
        {
            var media = await mediaService.UploadFileAsync(file);
            return Ok(media);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("retrieve")]
    public async Task<IActionResult> RetrieveFile([FromQuery] string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        try
        {
            var (fileStream, contentType) = await mediaService.GetFileAsync(fileName);
            return File(fileStream, contentType, fileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("delete")]
    public IActionResult DeleteFile([FromQuery] string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        try
        {
            mediaService.DeleteFile(fileName);
            return Ok("File deleted successfully");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}