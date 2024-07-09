using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zenvi.Core.Data.Context;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Utils;

namespace Zenvi.Core.Services;

public interface IPostService
{
    Task<Post> CreatePostAsync(ClaimsPrincipal user, string? content, List<string>? mediaNames);
    Task<List<Post>> GetAllPostsAsync();
    Task<Post> GetPostByIdAsync(int id);
    Task<Post> UpdatePostAsync(int id, ClaimsPrincipal user, string? content, List<string>? mediaNames);
    Task DeletePostAsync(int id, ClaimsPrincipal user);
    Task<Post> ReplyToPostAsync(ClaimsPrincipal user, Post post, List<string>? mediaNames, int repliedToId);
    Task<List<Post>> GetPostsFromFollowedUsersAsync(ClaimsPrincipal user);
}

public class PostService(ApplicationDbContext context) : IPostService
{
    private readonly LogHandler _logHandler = new(typeof(PostService));

    public async Task<Post> CreatePostAsync(ClaimsPrincipal user, string? content, List<string>? mediaNames)
    {
        _logHandler.LogInfo("Creating a new post.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logHandler.LogError("User ID not found in claims.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var postOp = await context.Users.FindAsync(userId);
        if (postOp == null)
        {
            _logHandler.LogError("Post operator not found.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        if (string.IsNullOrWhiteSpace(content) && (mediaNames == null || !mediaNames.Any()))
        {
            _logHandler.LogError("Either content or media must be provided.", new ArgumentException());
            throw new ArgumentException("Either content or media must be provided.");
        }

        var post = new Post
        {
            PostOp = postOp,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(content))
        {
            post.Content = content;
        }

        // Attach media to post
        if (mediaNames != null && mediaNames.Any())
        {
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.PostId = post.Id;
            }
            post.MediaContent = mediaContent;
        }

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("Post created successfully.");

        return post;
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        _logHandler.LogInfo("Retrieving all posts.");

        var posts = await context.Posts
            .Include(p => p.PostOp)
            .Include(p => p.MediaContent)
            .ToListAsync();

        _logHandler.LogInfo($"Retrieved {posts.Count} posts.");

        return posts;
    }

    public async Task<Post> GetPostByIdAsync(int id)
    {
        _logHandler.LogInfo($"Retrieving post with ID: {id}.");

        var post = await context.Posts
            .Include(p => p.PostOp)
            .Include(p => p.MediaContent)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            _logHandler.LogWarn($"Post with ID {id} not found.", new KeyNotFoundException());
            throw new KeyNotFoundException("Post not found");
        }

        _logHandler.LogInfo("Post retrieved successfully.");

        return post;
    }

    public async Task<Post> UpdatePostAsync(int id, ClaimsPrincipal user, string? content, List<string>? mediaNames)
    {
        _logHandler.LogInfo($"Updating post with ID: {id}.");

        // Ensure that at least one of updatedPost.Content or mediaNames is provided
        if (string.IsNullOrWhiteSpace(content) && (mediaNames == null || !mediaNames.Any()))
        {
            _logHandler.LogError("Either content or media must be provided.", new ArgumentException());
            throw new ArgumentException("Either content or media must be provided.");
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logHandler.LogError("User ID not found in claims.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var post = await context.Posts.Include(p => p.MediaContent).Include(post => post.PostOp).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
        {
            _logHandler.LogWarn($"Post with ID {id} not found.", new KeyNotFoundException());
            throw new KeyNotFoundException("Post not found");
        }

        if (post.PostOp.Id != userId)
        {
            _logHandler.LogError("User is not authorized to update this post.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            post.Content = content;
        }

        // Update media content
        if (mediaNames != null && mediaNames.Any())
        {
            // Clear current media
            post.MediaContent?.Clear();

            // Add new media
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.PostId = post.Id;
            }
            post.MediaContent = mediaContent;
        }

        post.UpdatedAt = DateTime.UtcNow;

        context.Posts.Update(post);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("Post updated successfully.");
        return post;
    }

    public async Task DeletePostAsync(int id, ClaimsPrincipal user)
    {
        _logHandler.LogInfo($"Deleting post with ID: {id}.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var post = await context.Posts
            .Include(p => p.PostOp)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
        {
            _logHandler.LogWarn($"Post with ID {id} not found.", new KeyNotFoundException());
            throw new KeyNotFoundException("Post not found");
        }

        if (post.PostOp.Id != userId)
        {
            _logHandler.LogError("User is not authorized to delete this post.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("Post deleted successfully.");
    }

    public async Task<Post> ReplyToPostAsync(ClaimsPrincipal user, Post post, List<string>? mediaNames, int repliedToId)
    {
        _logHandler.LogInfo("Creating a reply to a post.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logHandler.LogError("User ID not found in claims.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var postOp = await context.Users.FindAsync(userId);
        if (postOp == null)
        {
            _logHandler.LogError("Post operator not found.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var repliedToPost = await context.Posts.FindAsync(repliedToId);
        if (repliedToPost == null)
        {
            _logHandler.LogWarn($"Replied to post with ID {repliedToId} not found.", new KeyNotFoundException());
            throw new KeyNotFoundException("Replied to post not found");
        }

        post.PostOp = postOp;
        post.CreatedAt = DateTime.UtcNow;
        post.RepliedToId = repliedToId;

        // Attach media to post
        if (mediaNames != null && mediaNames.Any())
        {
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.PostId = post.Id;
            }
            post.MediaContent = mediaContent;
        }

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("Reply created successfully.");

        return post;
    }

    public async Task<List<Post>> GetPostsFromFollowedUsersAsync(ClaimsPrincipal user)
    {
        _logHandler.LogInfo("Retrieving posts from followed users.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logHandler.LogError("User ID not found in claims.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var followedUserIds = await context.Follows
            .Where(f => f.SourceId == userId)
            .Select(f => f.TargetId)
            .ToListAsync();

        var posts = await context.Posts
            .Where(p => followedUserIds.Contains(p.PostOp.Id))
            .Include(p => p.PostOp)
            .Include(p => p.MediaContent)
            .ToListAsync();

        _logHandler.LogInfo($"Retrieved {posts.Count} posts from followed users.");

        return posts;
    }
}