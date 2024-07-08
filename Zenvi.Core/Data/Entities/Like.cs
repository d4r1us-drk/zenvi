using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zenvi.Core.Data.Entities;

public class Like
{
    [Key]
    public int LikeId { get; set; }

    [Required]
    public int PostId { get; set; }

    [ForeignKey("PostId")]
    public Post Post { get; set; }

    [Required]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}