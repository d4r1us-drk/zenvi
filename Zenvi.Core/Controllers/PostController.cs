using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Services;
using Zenvi.Shared;

namespace Zenvi.Core.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostsController(IPostService postService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromBody] PostActionDto postActionDto)
    {
        try
        {
            var createdPost = await postService.CreatePostAsync(User, postActionDto.Content, postActionDto.MediaNames);
            return Ok(CreatedAtAction(nameof(GetPostById), new { id = createdPost.Id }, MapToPostDto(createdPost)));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetAllPosts()
    {
        var posts = await postService.GetAllPostsAsync();
        var postDtos = posts.Select(MapToPostDto).ToList();
        return Ok(postDtos);
    }

    [HttpGet("get/{id:int}")]
    public async Task<IActionResult> GetPostById(int id)
    {
        try
        {
            var post = await postService.GetPostByIdAsync(id);
            var postDto = MapToPostDto(post);
            return Ok(postDto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] PostActionDto postDto)
    {
        try
        {
            var updatedPst = await postService.UpdatePostAsync(id, User, postDto.Content, postDto.MediaNames);
            return Ok(CreatedAtAction(nameof(GetPostById), new { id = updatedPst.Id }, MapToPostDto(updatedPst)));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            await postService.DeletePostAsync(id, User);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("reply")]
    public async Task<IActionResult> ReplyToPost([FromBody] ReplyPostDto replyPostDto)
    {
        try
        {
            var post = new Post { Content = replyPostDto.Content };
            var createdPost = await postService.ReplyToPostAsync(User, post, replyPostDto.MediaNames, replyPostDto.RepliedToId);
            return Ok(CreatedAtAction(nameof(GetPostById), new { id = createdPost.Id }, MapToPostDto(createdPost)));
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

    [HttpGet("followed")]
    public async Task<IActionResult> GetPostsFromFollowedUsers()
    {
        try
        {
            var posts = await postService.GetPostsFromFollowedUsersAsync(User);
            var postDtos = posts.Select(MapToPostDto).ToList();
            return Ok(postDtos);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    private static PostDto MapToPostDto(Post post)
    {
        return new PostDto
        {
            PostId = post.Id,
            Content = post.Content ?? string.Empty,
            PostOpUserName = post.PostOp.UserName,
            PostOpName = $"{post.PostOp.Name} {post.PostOp.Surname}",
            MediaNames = post.MediaContent?.Select(m => m.Name).ToList(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            RepliedToId = post.RepliedToId
        };
    }
}