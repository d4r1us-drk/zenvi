using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Zenvi.Hub;
using Zenvi.Models;
using Zenvi.Services;
using Zenvi.Utils;

namespace Zenvi.Controllers;

[Authorize]
public class HomeController(UserManager<User> userManager, IPostService postService, ILikeService likeService, IMediaService mediaService, IHubContext<PostHub> hubContext)
    : Controller
{
    private readonly LogHandler _logHandler = new(typeof(HomeController));

    public async Task<IActionResult> Index()
    {
        var userId = userManager.GetUserId(User);
        var user = await userManager.Users
            .Include(u => u.ProfilePicture)
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var posts = await postService.GetAllPostsAsync();
        posts = posts.OrderByDescending(p => p.CreatedAt).ToList(); // Ensure posts are sorted by most recent

        var likedPostIds = await likeService.GetLikedPostIdsAsync(User);

        var model = new HomeViewModel
        {
            User = user,
            Posts = posts,
            LikedPostIds = likedPostIds
        };

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

    [HttpPost]
    public async Task<IActionResult> LikePost(int postId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        await likeService.LikePostAsync(User, postId);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLikePost(int postId)
    {
        try
        {
            await likeService.ToggleLikePostAsync(User, postId);
        }
        catch (Exception ex)
        {
            _logHandler.LogError($"Error toggling like for post {postId}: {ex.Message}", ex);
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> GetPosts(int page = 1, int pageSize = 10)
    {
        var posts = await postService.GetPostsPagedAsync(page, pageSize);
        var likedPostIds = await likeService.GetLikedPostIdsAsync(User);
        var model = new HomeViewModel
        {
            Posts = posts,
            LikedPostIds = likedPostIds
        };
        return PartialView("_PostListPartial", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
