namespace Zenvi.Server.Entities;

public class Post
{
    public int Id { get; set; }

    public required User PostOp { get; set; }

    public string? Content { get; set; }

    public int LikeCount { get; set; }

    public List<Media>? MediaContent { get; set; }

    public int? RepliedToId { get; set; }

    public Post? RepliedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public List<Like> Likes { get; set; } = new();
}
