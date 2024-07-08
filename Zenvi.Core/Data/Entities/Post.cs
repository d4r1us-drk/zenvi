using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zenvi.Core.Data.Entities;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    public User PostOp { get; set; }

    [StringLength(5000)]
    public string? Content { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "LikeCount must be a non-negative integer.")]
    public int LikeCount { get; set; }

    public List<Media>? MediaContent { get; set; }

    public int? RepliedToId { get; set; }

    [ForeignKey("RepliedToId")]
    public Post? RepliedTo { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [DataType(DataType.DateTime)]
    public DateTime? UpdatedAt { get; set; }
}