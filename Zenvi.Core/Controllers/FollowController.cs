using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Core.Services;
using Zenvi.Shared;

namespace Zenvi.Core.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FollowController(IFollowService followService) : ControllerBase
{
    [HttpPost("follow")]
    public async Task<IActionResult> FollowUser([FromBody] FollowDto followDto)
    {
        try
        {
            await followService.FollowUserAsync(User, followDto.TargetUserId);
            return NoContent();
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

    [HttpPost("unfollow")]
    public async Task<IActionResult> UnfollowUser([FromBody] FollowDto unfollowDto)
    {
        try
        {
            await followService.UnfollowUserAsync(User, unfollowDto.TargetUserId);
            return NoContent();
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

    [HttpGet("followers/{userId}")]
    public async Task<IActionResult> GetFollowers(string userId)
    {
        var followers = await followService.GetFollowersAsync(userId);
        var followerDtos = followers.Select(f => new FollowerDto
        {
            FollowerUserName = f.Source.UserName,
            FollowerName = $"{f.Source.Name} {f.Source.Surname}",
            FollowerUserId = f.SourceId,
            FollowedAt = f.FollowedAt
        }).ToList();

        return Ok(followerDtos);
    }

    [HttpGet("following/{userId}")]
    public async Task<IActionResult> GetFollowing(string userId)
    {
        var following = await followService.GetFollowingAsync(userId);
        var followingDtos = following.Select(f => new FollowingDto
        {
            FollowingUserName = f.Target.UserName,
            FollowingName = $"{f.Target.Name} {f.Target.Surname}",
            FollowingUserId = f.TargetId,
            FollowedAt = f.FollowedAt
        }).ToList();

        return Ok(followingDtos);
    }

    [HttpGet("are-following-each-other/{otherUserId}")]
    public async Task<IActionResult> AreUsersFollowingEachOther(string otherUserId)
    {
        try
        {
            var result = await followService.AreUsersFollowingEachOtherAsync(User, otherUserId);
            return Ok(new { AreFollowingEachOther = result });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
