using System.ComponentModel.DataAnnotations;

namespace Zenvi.Models;

public class Conversation
{
    [Key]
    public int ConversationId { get; set; }

    [Required]
    public User User1 { get; set; }

    [Required]
    public User User2 { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Message> Messages { get; set; } = new();
}