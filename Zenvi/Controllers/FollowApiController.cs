using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Services;

namespace Zenvi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FollowApiController(IFollowService followService) : ControllerBase
{
    [HttpPost("Follow")]
    public async Task<IActionResult> Follow([FromBody] string targetUserId)
    {
        try
        {
            if (await followService.AreUsersFollowingEachOtherAsync(User, targetUserId))
            {
                return BadRequest(new { error = "Already following this user" });
            }

            await followService.FollowUserAsync(User, targetUserId);
            return Ok(new { isFollowing = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("Unfollow")]
    public async Task<IActionResult> Unfollow([FromBody] string targetUserId)
    {
        try
        {
            if (!await followService.IsFollowingAsync(User, targetUserId))
            {
                return BadRequest(new { error = "Not following this user" });
            }

            await followService.UnfollowUserAsync(User, targetUserId);
            return Ok(new { isFollowing = false });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}