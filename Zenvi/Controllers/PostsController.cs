using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenvi.Models;
using Zenvi.Services;

public class PostsController : Controller
{
    private readonly UserManager<User> userManager;
    private readonly IPostService postService;
    private readonly ILikeService likeService;

    public PostsController(UserManager<User> userManager, IPostService postService, ILikeService likeService)
    {
        this.userManager = userManager;
        this.postService = postService;
        this.likeService = likeService;
    }

    public async Task<IActionResult> UserPosts()
    {
        var userId = userManager.GetUserId(User);
        var user = await userManager.Users
            .Include(u => u.ProfilePicture)
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var posts = await postService.GetPostsByUserAsync(user);
        var likedPostIds = await likeService.GetLikedPostIdsAsync(User);

        var model = new UserPostsViewModel
        {
            User = user,
            Posts = posts,
            LikedPostIds = likedPostIds.ToList()
        };

        return View(model);
    }
}
