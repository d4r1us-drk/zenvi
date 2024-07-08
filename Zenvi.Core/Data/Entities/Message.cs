using System.ComponentModel.DataAnnotations;

namespace Zenvi.Core.Data.Entities;

public class Message
{
    [Key]
    public int MessageId { get; set; }

    [Required]
    public User Sender { get; set; }

    [Required]
    public User Receiver { get; set; }

    [StringLength(5000)]
    public string Content { get; set; }

    public List<Media>? MediaContent { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime SentAt { get; set; } = DateTime.Now;

    [DataType(DataType.DateTime)]
    public DateTime? ReadAt { get; set; }
}