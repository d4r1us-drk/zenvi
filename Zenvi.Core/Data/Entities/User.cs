using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Zenvi.Core.Data.Entities;

// User model class inherits from IdentityUser, which already has email and password data defined
public class User : IdentityUser
{
    [Required]
    [StringLength(50)]
    public string? Name { get; set; }

    [Required]
    [StringLength(50)]
    public string? Surname { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool Banned { get; set; }

    public Media? ProfilePicture { get; set; }

    public Media? BannerPicture { get; set; }

    [NotMapped]
    public new string? PhoneNumber { get; set; }

    [NotMapped]
    public new bool PhoneNumberConfirmed { get; set; }
}