using Zenvi.Core.Utils;

namespace Zenvi.Core.Services;

public interface IMediaUploadService
{
    Task<string> UploadFileAsync(IFormFile file);
}

public class MediaUploadService : IMediaUploadService
{
    private readonly string _storagePath;
    private readonly LogHandler<MediaUploadService> _logHandler = new();
    private readonly int _maxFileSize = 1024 * 1024 * 10; // 10MB max

    public MediaUploadService(DataHandlerService dataHandlerService)
    {
        _storagePath = Path.Combine(dataHandlerService.GetBasePath(), "uploads");
        _logHandler.LogInfo($"Media Upload Service initialized with path: {_storagePath}");
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null)
        {
            throw new ArgumentException("File is null", nameof(file));
        }

        if (file.Length > _maxFileSize)
        {
            throw new ArgumentException("File is too large", nameof(file));
        }

        var relativePath = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(file.FileName));
        var fullPath = Path.Combine(_storagePath, relativePath);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Path is null"));
        }
        catch (Exception ex)
        {
            _logHandler.LogError("An error occurred while trying to create data directory.", ex);
            throw;
        }

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return relativePath;
    }
}
