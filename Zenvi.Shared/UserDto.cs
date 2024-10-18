namespace Zenvi.Shared;

public class UserDto
{
    public required string Email { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string? Bio { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool Banned { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? BannerPictureUrl { get; set; }
}