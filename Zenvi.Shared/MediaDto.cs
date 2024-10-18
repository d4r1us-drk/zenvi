namespace Zenvi.Shared;

public class MediaDto
{
    public string Name { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    // This property will store the raw bytes of the file for transfer to the CDN
    public byte[] FileData { get; set; } = [];
}