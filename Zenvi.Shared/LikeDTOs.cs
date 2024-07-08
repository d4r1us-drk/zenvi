using System.ComponentModel.DataAnnotations;

namespace Zenvi.Shared;

public class LikeDto
{
    [Required]
    public int PostId { get; set; }
}