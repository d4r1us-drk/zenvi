using Zenvi.Data;
using Zenvi.Data.Models;

namespace Zenvi.Services;

public interface IMediaService
{
    Task<Media> SaveMediaAsync(Stream mediaStream, MediaType mediaType);
    Task<Media?> GetMediaAsync(int mediaId);
}

public class MediaService(ApplicationDbContext context) : IMediaService
{
    public async Task<Media> SaveMediaAsync(Stream mediaStream, MediaType mediaType)
    {
        using var memoryStream = new MemoryStream();
        await mediaStream.CopyToAsync(memoryStream);

        var media = new Media
        {
            MediaBlob = memoryStream.ToArray(),
            Type = mediaType,
            CreatedAt = DateTime.UtcNow
        };

        context.Media.Add(media);
        await context.SaveChangesAsync();

        return media;
    }

    public async Task<Media?> GetMediaAsync(int mediaId)
    {
        return await context.Media.FindAsync(mediaId);
    }
}