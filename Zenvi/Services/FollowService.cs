using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zenvi.Data;
using Zenvi.Models;
using Zenvi.Utils;

namespace Zenvi.Services;

public interface IFollowService
{
    Task FollowUserAsync(ClaimsPrincipal user, string targetUserId);
    Task UnfollowUserAsync(ClaimsPrincipal user, string targetUserId);
    Task<List<Follow>> GetFollowersAsync(string userId);
    Task<List<Follow>> GetFollowingAsync(string userId);
    Task<bool> AreUsersFollowingEachOtherAsync(ClaimsPrincipal user, string otherUserId);
}

public class FollowService(ApplicationDbContext context) : IFollowService
{
    private readonly LogHandler _logHandler = new(typeof(FollowService));

    public async Task FollowUserAsync(ClaimsPrincipal user, string targetUserId)
    {
        _logHandler.LogInfo("Attempting to follow a user.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || userId == targetUserId)
        {
            _logHandler.LogError("Unauthorized follow attempt.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var sourceUser = await context.Users.FindAsync(userId);
        var targetUser = await context.Users.FindAsync(targetUserId);

        if (sourceUser == null || targetUser == null)
        {
            _logHandler.LogError("User not found.", new KeyNotFoundException("User not found"));
            throw new KeyNotFoundException("User not found");
        }

        var follow = new Follow
        {
            SourceId = userId,
            TargetId = targetUserId,
            FollowedAt = DateTime.UtcNow
        };

        context.Follows.Add(follow);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("User followed successfully.");
    }

    public async Task UnfollowUserAsync(ClaimsPrincipal user, string targetUserId)
    {
        _logHandler.LogInfo("Attempting to unfollow a user.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || userId == targetUserId)
        {
            _logHandler.LogError("Unauthorized unfollow attempt.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var follow = await context.Follows
            .FirstOrDefaultAsync(f => f.SourceId == userId && f.TargetId == targetUserId);

        if (follow == null)
        {
            _logHandler.LogWarn("Follow relationship not found.", new KeyNotFoundException("Follow relationship not found"));
            throw new KeyNotFoundException("Follow relationship not found");
        }

        context.Follows.Remove(follow);
        await context.SaveChangesAsync();

        _logHandler.LogInfo("User unfollowed successfully.");
    }

    public async Task<List<Follow>> GetFollowersAsync(string userId)
    {
        _logHandler.LogInfo($"Retrieving followers for user ID: {userId}.");

        var followers = await context.Follows
            .Where(f => f.TargetId == userId)
            .Include(f => f.Source)
            .ToListAsync();

        _logHandler.LogInfo($"Retrieved {followers.Count} followers for user ID: {userId}.");

        return followers;
    }

    public async Task<List<Follow>> GetFollowingAsync(string userId)
    {
        _logHandler.LogInfo($"Retrieving following list for user ID: {userId}.");

        var following = await context.Follows
            .Where(f => f.SourceId == userId)
            .Include(f => f.Target)
            .ToListAsync();

        _logHandler.LogInfo($"Retrieved {following.Count} following for user ID: {userId}.");

        return following;
    }

    public async Task<bool> AreUsersFollowingEachOtherAsync(ClaimsPrincipal user, string otherUserId)
    {
        _logHandler.LogInfo("Checking if users are following each other.");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            _logHandler.LogError("User ID not found in claims.", new UnauthorizedAccessException());
            throw new UnauthorizedAccessException();
        }

        var isFollowing = await context.Follows
            .AnyAsync(f => f.SourceId == userId && f.TargetId == otherUserId);

        var isFollowedBy = await context.Follows
            .AnyAsync(f => f.SourceId == otherUserId && f.TargetId == userId);

        _logHandler.LogInfo($"Users follow each other: {isFollowing && isFollowedBy}.");

        return isFollowing && isFollowedBy;
    }
}