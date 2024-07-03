using System.ComponentModel.DataAnnotations;

namespace Zenvi.Data.Models;

public class Media
{
    [Key]
    public int MediaID { get; set; }

    [Required]
    public byte[] MediaBlob { get; set; }

    [Required]
    public MediaType Type { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}

public enum MediaType
{
    Png,
    Jpg,
    Gif,
    Mp4
}