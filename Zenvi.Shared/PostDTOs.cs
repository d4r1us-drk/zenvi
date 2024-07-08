using System.ComponentModel.DataAnnotations;

namespace Zenvi.Shared;

public class CreatePostDto
{
    [Required]
    [StringLength(5000)]
    public string? Content { get; set; }

    public List<string>? MediaNames { get; set; }
}

public class UpdatePostDto
{
    public string? Content { get; set; }

    public List<string>? MediaNames { get; set; }
}

public class ReplyPostDto
{
    [Required]
    [StringLength(5000)]
    public string? Content { get; set; }

    public List<string>? MediaNames { get; set; }

    [Required]
    public int RepliedToId { get; set; }
}

public class PostDto
{
    public int PostId { get; set; }
    public string Content { get; set; }
    public string PostOpUserName { get; set; }
    public string PostOpName { get; set; }
    public List<string>? MediaNames { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? RepliedToId { get; set; }
}