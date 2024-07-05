using Zenvi.Core.Data.Entities;

namespace Zenvi.Core.Identity.Models;

public class InfoModel
{
    public string? Email { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Bio { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool Banned { get; set; }
    public Media? ProfilePictureUrl { get; set; }
    public Media? BannerPictureUrl { get; set; }
}