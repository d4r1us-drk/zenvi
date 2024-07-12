using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenvi.Models;
using Zenvi.Services;

namespace Zenvi.Controllers;

[Authorize]
public class SearchController(UserManager<User> userManager, IPostService postService, ILikeService likeService)
    : Controller
{
    private readonly ILikeService likeService = likeService;

    public async Task<IActionResult> Index(string query = "", int page = 1, int pageSize = 10)
    {
        List<User> users = [];
        List<Post> posts = [];

        if (!string.IsNullOrEmpty(query))
        {
            users = await userManager.Users
                .Where(u => u.UserName.Contains(query) || u.Name.Contains(query) || u.Surname.Contains(query))
                .Include(u => u.ProfilePicture) // Include ProfilePicture
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            posts = await postService.SearchPostsAsync(query, page, pageSize);
        }

        var model = new SearchViewModel
        {
            Query = query,
            Users = users,
            Posts = posts,
            Page = page,
            PageSize = pageSize
        };

        return View(model);
    }
}