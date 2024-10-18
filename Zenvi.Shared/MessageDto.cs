namespace Zenvi.Shared;

public class MessageDto
{
    public int MessageId { get; set; }

    public required string MessageOpEmail { get; set; }

    public string? MessageOpName { get; set; }

    public int ConversationId { get; set; }

    public string? Content { get; set; }

    public List<MediaDto>? MediaContent { get; set; }

    public List<LikeDto>? Likes { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? RepliedToMessageId { get; set; }
}