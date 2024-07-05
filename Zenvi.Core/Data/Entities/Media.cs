using System.ComponentModel.DataAnnotations;

namespace Zenvi.Core.Data.Entities;

public class Media
{
    [Key]
    public int MediaId { get; set; }

    [Required]
    [StringLength(500)]
    public string MediaUrl { get; set; }

    [Required]
    public MediaType MediaType { get; set; }
}

public enum MediaType
{
    Photo,
    Video
}