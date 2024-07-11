using System.ComponentModel.DataAnnotations;

namespace Zenvi.Models;

public class Media
{
    [Key]
    [StringLength(50)]
    public required string Name { get; set; }

    [Required]
    public required string Type { get; set; }

    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? MessageId { get; set; }
    public Message? Message { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}