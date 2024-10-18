namespace Zenvi.Shared;

public class PostDto
{
    public int Id { get; set; }

    public required string PostOpEmail { get; set; }

    public string? PostOpName { get; set; }

    public string? Content { get; set; }

    public int LikeCount { get; set; }

    public List<MediaDto>? MediaContent { get; set; }

    public List<LikeDto> Likes { get; set; } = new();

    public int? RepliedToId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}