using System.ComponentModel.DataAnnotations;

namespace Zenvi.Core.Identity.Models;

public class UpdateModel
{
    [EmailAddress]
    public string? NewEmail { get; set; }
    public bool IsEmailConfirmed { get; set; }

    public string? NewPassword { get; set; }
    public string? OldPassword { get; set; }

    [StringLength(50)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public string? DateOfBirth { get; set; }

    public bool Banned { get; set; }

    public IFormFile? ProfilePicture { get; set; }
    public IFormFile? BannerPicture { get; set; }
}