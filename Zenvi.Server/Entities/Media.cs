namespace Zenvi.Server.Entities;

public class Media : IBaseEntity<Guid>
{
    public required Guid Guid { get; set; } = Guid.NewGuid();

    Guid IBaseEntity<Guid>.Id
    {
        get => Guid;
        set => Guid = value;
    }

    public required string Name { get; set; }

    public required string MimeType { get; set; }

    public string? MediaUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
