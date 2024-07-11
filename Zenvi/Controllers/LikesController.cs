using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Services;

namespace Zenvi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LikesApiController(ILikeService likeService, IPostService postService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [Route("ToggleLikePost")]
    public async Task<IActionResult> ToggleLikePost([FromBody] int postId)
    {
        await likeService.ToggleLikePostAsync(User, postId);
        var post = await postService.GetPostByIdAsync(postId);
        return Ok(new { post.LikeCount });
    }
}