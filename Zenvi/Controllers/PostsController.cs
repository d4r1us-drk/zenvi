using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Zenvi.Hub;
using Zenvi.Models;
using Zenvi.Services;
using Zenvi.Utils;

namespace Zenvi.Controllers;

[Authorize]
public class PostsController(
    UserManager<User> userManager,
    IPostService postService,
    ILikeService likeService,
    IFollowService followService,
    IMediaService mediaService,
    IHubContext<PostHub> hubContext)
    : Controller
{
    private readonly LogHandler _logHandler = new(typeof(PostsController));

    public async Task<IActionResult> UserPosts(string userId, int page = 1, int pageSize = 5)
    {
        if (string.IsNullOrEmpty(userId))
        {
            userId = userManager.GetUserId(User);
        }

        var user = await userManager.Users
            .Include(u => u.ProfilePicture)
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound();
        }

        var posts = await postService.GetPostsByUserPagedAsync(user, page, pageSize);
        var likedPostIds = await likeService.GetLikedPostIdsAsync(User);
        var currentUserId = userManager.GetUserId(User);
        var isFollowing = await followService.IsFollowingAsync(User, userId);

        var model = new UserPostsViewModel
        {
            User = user,
            Posts = posts,
            LikedPostIds = likedPostIds.ToList(),
            Page = page,
            PageSize = pageSize,
            IsCurrentUser = currentUserId == userId,
            IsFollowing = isFollowing
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_PostListPartial", model.Posts);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost(string content, List<IFormFile> mediaFiles)
    {
        try
        {
            var mediaNames = new List<string>();
            foreach (var file in mediaFiles)
            {
                if (file.Length > 0)
                {
                    if (file.ContentType.StartsWith("image") || file.ContentType.StartsWith("video"))
                    {
                        var media = await mediaService.UploadFileAsync(file);
                        mediaNames.Add(media.Name);
                    }
                }
            }

            await postService.CreatePostAsync(User, content, mediaNames);

            // Notify all clients that a new post is available
            await hubContext.Clients.All.SendAsync("ReceiveNewPostNotification");
        }
        catch (Exception ex)
        {
            _logHandler.LogError($"Error creating post: {ex.Message}", ex);
        }

        return RedirectToAction("Index");
    }


    public async Task<IActionResult> ViewPost(int postId)
    {
        var post = await postService.GetPostByIdAsync(postId);

        var replies = await postService.GetRepliesAsync(postId);
        var likedPostIds = await likeService.GetLikedPostIdsAsync(User);

        var model = new PostViewModel
        {
            Post = post,
            Replies = replies,
            LikedPostIds = likedPostIds.ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReply(int repliedToId, string content, List<IFormFile> mediaFiles)
    {
        try
        {
            var mediaNames = new List<string>();
            foreach (var file in mediaFiles)
            {
                if (file.Length > 0)
                {
                    if (file.ContentType.StartsWith("image") || file.ContentType.StartsWith("video"))
                    {
                        var media = await mediaService.UploadFileAsync(file);
                        mediaNames.Add(media.Name);
                    }
                }
            }

            var post = await postService.ReplyToPostAsync(User, content, mediaNames, repliedToId);

            // Notify all clients that a new reply is available
            await hubContext.Clients.All.SendAsync("ReceiveNewReplyNotification", repliedToId);

            return RedirectToAction("ViewPost", new { postId = repliedToId });
        }
        catch (Exception ex)
        {
            _logHandler.LogError($"Error creating reply: {ex.Message}", ex);
            return RedirectToAction("ViewPost", new { postId = repliedToId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> FollowUser(string targetUserId)
    {
        await followService.FollowUserAsync(User, targetUserId);
        return RedirectToAction(nameof(UserPosts), new { userId = targetUserId });
    }

    [HttpPost]
    public async Task<IActionResult> UnfollowUser(string targetUserId)
    {
        await followService.UnfollowUserAsync(User, targetUserId);
        return RedirectToAction(nameof(UserPosts), new { userId = targetUserId });
    }
}