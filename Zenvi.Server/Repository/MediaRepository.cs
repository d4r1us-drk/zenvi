using Zenvi.Server.Entities;

namespace Zenvi.Server.Repository;

public class MediaRepository(IRepository<Media, Guid> mediaRepository)
{
    public async Task<IEnumerable<Media>> GetAllMediasAsync()
    {
        return await mediaRepository.GetAllAsync();
    }

    public async Task<Media?> GetMediaByIdAsync(Guid guid)
    {
        return await mediaRepository.GetByIdAsync(guid);
    }

    public async Task AddUserAsync(Media media)
    {
        await mediaRepository.AddAsync(media);
    }

    public async Task UpdateMediaAsync(Media media)
    {
        await mediaRepository.UpdateAsync(media);
    }

    public async Task DeleteMediaAsync(Guid guid)
    {
        await mediaRepository.DeleteAsync(guid);
    }
}