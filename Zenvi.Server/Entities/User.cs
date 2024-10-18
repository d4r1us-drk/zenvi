namespace Zenvi.Server.Entities;

public class User : IUpdatableEntity<int>
{
    public required int Id { get; set; }

    public required string Email { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string? Bio { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public bool Banned { get; set; }

    public Media? ProfilePicture { get; set; }

    public Media? BannerPicture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
