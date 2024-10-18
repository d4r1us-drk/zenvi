namespace Zenvi.Server.Entities;

public class Follow
{
    public required User Source { get; set; }

    public required User Target { get; set; }

    public DateTime FollowedAt { get; set; } = DateTime.Now;
}
