using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Core.Services;
using Zenvi.Shared;

namespace Zenvi.Core.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LikeController(ILikeService likeService) : ControllerBase
{
    [HttpPost("like")]
    public async Task<IActionResult> LikePost([FromBody] LikeDto likeDto)
    {
        try
        {
            await likeService.LikePostAsync(User, likeDto.PostId);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("unlike")]
    public async Task<IActionResult> UnlikePost([FromBody] LikeDto likeDto)
    {
        try
        {
            await likeService.UnlikePostAsync(User, likeDto.PostId);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}