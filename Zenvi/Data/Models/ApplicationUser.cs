using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Zenvi.Data.Models;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? Surname { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(5000)]
    public string? Bio { get; set; }

    public bool Banned { get; set; }

    public Media? ProfilePicture { get; set; }

    public Media? BannerPicture { get; set; }
}