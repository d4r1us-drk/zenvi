using Microsoft.AspNetCore.Components.Forms;
using Zenvi.Utils;

namespace Zenvi.Services;

public interface IMediaUploadService
{
    Task<string> UploadFileAsync(IBrowserFile file);
}

public class MediaUploadService : IMediaUploadService
{
    private readonly string _storagePath;
    private readonly LogHandler<MediaUploadService> _logHandler = new();

    public MediaUploadService(DataHandlerService dataHandlerService, ILogger<MediaUploadService> logger)
    {
        _storagePath = Path.Combine(dataHandlerService.GetBasePath(), "uploads");
        _logHandler.LogInfo($"Media Upload Service initialized with path: {_storagePath}");
    }

    public async Task<string> UploadFileAsync(IBrowserFile file)
    {
        if (file == null)
        {
            throw new ArgumentException("File is null", nameof(file));
        }

        var relativePath = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(file.Name));
        var fullPath = Path.Combine(_storagePath, relativePath);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        }
        catch (Exception ex)
        {
            _logHandler.LogError("An error occurred while trying to create data directory.", ex);
        }

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.OpenReadStream().CopyToAsync(stream);

        return relativePath;
    }
}