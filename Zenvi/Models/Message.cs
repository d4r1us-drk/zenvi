using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zenvi.Models;

public class Message
{
    [Key]
    public int MessageId { get; set; }

    [Required]
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; }

    [StringLength(5000)]
    public string Content { get; set; }

    public List<Media>? MediaContent { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime SentAt { get; set; } = DateTime.Now;

    [DataType(DataType.DateTime)]
    public DateTime? ReadAt { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? UpdatedAt { get; set; }

    public int? RepliedToId { get; set; }

    [ForeignKey("RepliedToId")]
    public Message? RepliedTo { get; set; }
}
