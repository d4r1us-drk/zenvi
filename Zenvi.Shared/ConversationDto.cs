namespace Zenvi.Shared;

public class ConversationDto
{
    public int ConversationId { get; set; }

    public required List<string> ParticipantEmails { get; set; } = new();

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<MessageDto>? Messages { get; set; }
}