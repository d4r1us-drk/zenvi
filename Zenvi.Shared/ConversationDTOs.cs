using System.ComponentModel.DataAnnotations;

namespace Zenvi.Shared;

public class CreateConversationDto
{
    [Required]
    public string User2UserName { get; set; }

    public string? Description { get; set; }
}

public class UpdateConversationDto
{
    public string Description { get; set; }
}

public class ConversationDto
{
    public int ConversationId { get; set; }
    public string? User1UserName { get; set; }
    public string? User2UserName { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MessageDto> Messages { get; set; }
}

public class MessageDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public string Content { get; set; }
    public List<string>? MediaNames { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? RepliedToId { get; set; }
}

public class CreateMessageDto
{
    [Required]
    public int ConversationId { get; set; }
    public string Content { get; set; }
    public List<string> MediaNames { get; set; }
}

public class UpdateMessageDto
{
    public string Content { get; set; }
    public List<string> MediaNames { get; set; }
}

public class ReplyMessageDto
{
    [Required]
    public int ConversationId { get; set; }
    public string Content { get; set; }
    public List<string> MediaNames { get; set; }
    public int RepliedToId { get; set; }
}
