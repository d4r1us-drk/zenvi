using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zenvi.Core.Data.Context;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Utils;

namespace Zenvi.Core.Services;

public interface ILikeService
{
    Task LikePostAsync(ClaimsPrincipal user, int postId);
    Task UnlikePostAsync(ClaimsPrincipal user, int postId);
}
public class LikeService(ApplicationDbContext context) : ILikeService
{
    private readonly LogHandler _logHandler = new(typeof(LikeService));

    public async Task LikePostAsync(ClaimsPrincipal user, int postId)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            _logHandler.LogWarn($"Post with id {postId} not found.", new KeyNotFoundException($"Post with id {postId} not found."));
            throw new KeyNotFoundException("Post not found");
        }

        if (await context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId))
        {
            _logHandler.LogWarn($"User {userId} has already liked post {postId}.", new InvalidOperationException($"User {userId} has already liked post {postId}."));
            throw new InvalidOperationException("Post already liked");
        }

        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        context.Likes.Add(like);
        post.LikeCount += 1;
        await context.SaveChangesAsync();

        _logHandler.LogInfo($"User {userId} liked post {postId}.");
    }

    public async Task UnlikePostAsync(ClaimsPrincipal user, int postId)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            _logHandler.LogWarn($"Post with id {postId} not found.", new KeyNotFoundException($"Post with id {postId} not found."));
            throw new KeyNotFoundException("Post not found");
        }

        var like = await context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (like == null)
        {
            _logHandler.LogWarn($"Like by user {userId} on post {postId} not found.", new KeyNotFoundException($"Like by user {userId} on post {postId} not found."));
            throw new KeyNotFoundException("Like not found");
        }

        context.Likes.Remove(like);
        post.LikeCount -= 1;
        await context.SaveChangesAsync();

        _logHandler.LogInfo($"User {userId} unliked post {postId}.");
    }
}