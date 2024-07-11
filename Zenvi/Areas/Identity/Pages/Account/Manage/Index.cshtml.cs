using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Zenvi.Models;
using Zenvi.Services;

namespace Zenvi.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IMediaService _mediaService;

    public IndexModel(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IMediaService mediaService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mediaService = mediaService;
    }

    public string Username { get; set; }

    public string ProfilePicturePath { get; set; }
    public string BannerPicturePath { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string? Surname { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public IFormFile? BannerPicture { get; set; }
    }

    private async Task LoadAsync(User user)
    {
        var userName = await _userManager.GetUserNameAsync(user);

        Username = userName;

        Input = new InputModel
        {
            Name = user.Name,
            Surname = user.Surname,
            Bio = user.Bio
        };

        ProfilePicturePath = user.ProfilePicture != null
            ? Url.Content($"~/media/{user.ProfilePicture.Name}")
            : Url.Content("~/images/profile-picture-placeholder.png");

        BannerPicturePath = user.BannerPicture != null
            ? Url.Content($"~/media/{user.BannerPicture.Name}")
            : Url.Content("~/images/banner-picture-placeholder.png");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.Users
            .Include(u => u.ProfilePicture)
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.Users
            .Include(u => u.ProfilePicture)
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        if (Input.ProfilePicture != null)
        {
            if (!IsValidImage(Input.ProfilePicture))
            {
                ModelState.AddModelError("Input.ProfilePicture", "Please upload a valid image file (jpg, jpeg, png, gif).");
                await LoadAsync(user);
                return Page();
            }
            var profileMedia = await _mediaService.UploadFileAsync(Input.ProfilePicture);
            user.ProfilePicture = profileMedia;
        }

        if (Input.BannerPicture != null)
        {
            if (!IsValidImage(Input.BannerPicture))
            {
                ModelState.AddModelError("Input.BannerPicture", "Please upload a valid image file (jpg, jpeg, png, gif).");
                await LoadAsync(user);
                return Page();
            }
            var bannerMedia = await _mediaService.UploadFileAsync(Input.BannerPicture);
            user.BannerPicture = bannerMedia;
        }

        user.Name = Input.Name;
        user.Surname = Input.Surname;

        if (!string.IsNullOrWhiteSpace(Input.Bio))
        {
            user.Bio = Input.Bio;
        }

        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteProfilePictureAsync()
    {
        var user = await _userManager.Users
            .Include(u => u.ProfilePicture)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (user.ProfilePicture != null)
        {
            _mediaService.DeleteFile(user.ProfilePicture.Name);
            user.ProfilePicture = null;
            await _userManager.UpdateAsync(user);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteBannerPictureAsync()
    {
        var user = await _userManager.Users
            .Include(u => u.BannerPicture)
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (user.BannerPicture != null)
        {
            _mediaService.DeleteFile(user.BannerPicture.Name);
            user.BannerPicture = null;
            await _userManager.UpdateAsync(user);
        }

        return RedirectToPage();
    }

    private bool IsValidImage(IFormFile file)
    {
        var validTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        return validTypes.Contains(file.ContentType.ToLower());
    }
}