namespace Zenvi.Server.Entities;

public class Message
{
    public int MessageId { get; set; }

    public required User MessageOp { get; set; }

    public required Conversation Conversation { get; set; }

    public string? Content { get; set; }

    public List<Media>? MediaContent { get; set; }

    public List<Like>? Likes { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public DateTime? ReadAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Message? RepliedTo { get; set; }
}
