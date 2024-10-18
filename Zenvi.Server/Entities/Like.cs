namespace Zenvi.Server.Entities;

public class Like
{
    public int Id { get; set; }

    public required Post Post { get; set; }

    public required User User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
