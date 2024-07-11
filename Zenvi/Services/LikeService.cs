using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zenvi.Data;
using Zenvi.Models;
using Zenvi.Utils;

namespace Zenvi.Services;

public interface ILikeService
{
    Task LikePostAsync(ClaimsPrincipal user, int postId);
    Task UnlikePostAsync(ClaimsPrincipal user, int postId);
    Task<HashSet<int>> GetLikedPostIdsAsync(ClaimsPrincipal user);
    Task ToggleLikePostAsync(ClaimsPrincipal user, int postId);
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

    public async Task<HashSet<int>> GetLikedPostIdsAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var likedPostIdsList = await context.Likes
            .Where(l => l.UserId == userId)
            .Select(l => l.PostId)
            .ToListAsync();

        var likedPostIds = new HashSet<int>(likedPostIdsList);

        return likedPostIds;
    }

    public async Task ToggleLikePostAsync(ClaimsPrincipal user, int postId)
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
        if (like != null)
        {
            // Unlike the post
            context.Likes.Remove(like);
            post.LikeCount -= 1;
        }
        else
        {
            // Like the post
            like = new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Likes.Add(like);
            post.LikeCount += 1;
        }

        await context.SaveChangesAsync();
        _logHandler.LogInfo($"User {userId} toggled like on post {postId}.");
    }
}