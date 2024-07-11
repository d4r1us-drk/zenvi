using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zenvi.Models;

public class Follow
{
    [Required]
    public User Source { get; set; }

    [Required]
    public User Target { get; set; }

    public DateTime FollowedAt { get; set; } = DateTime.Now;

    [Required, ForeignKey("Source")]
    public string SourceId { get; set; }

    [Required, ForeignKey("Target")]
    public string TargetId { get; set; }
}