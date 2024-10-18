namespace Zenvi.Shared;

public class LikeDto
{
    public int Id { get; set; }

    public required string UserEmail { get; set; }

    public string? UserName { get; set; }

    public DateTime CreatedAt { get; set; }
}