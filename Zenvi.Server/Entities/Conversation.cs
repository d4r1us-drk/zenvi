namespace Zenvi.Server.Entities;

public class Conversation
{
    public int ConversationId { get; set; }

    public List<User> Users { get; set; } = new();

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Message> Messages { get; set; } = new();
}
