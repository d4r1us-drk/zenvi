using Zenvi.Core.Data.Context;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Utils;

namespace Zenvi.Core.Services;

public interface IMediaService
{
    Task<Media> UploadFileAsync(IFormFile file);
    Task<(Stream fileStream, string contentType)> GetFileAsync(string fileName);
    void DeleteFile(string fileName);
}

public class MediaService : IMediaService
{
    private readonly string _storagePath;
    private readonly LogHandler _logHandler = new(typeof(MediaService));
    private const int MaxFileSize = 1024 * 1024 * 10; // 10MB max
    private readonly ApplicationDbContext _context;

    public MediaService(IHostEnvironment environment, IConfiguration configuration, ApplicationDbContext context)
    {
        _storagePath = environment.IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), "TempData", "uploads")
            : Path.Combine(configuration.GetValue<string>("DataSettings:BasePath") ?? throw new InvalidOperationException("Data path not set in appsettings.json"), "uploads");

        _context = context;

        try
        {
            Directory.CreateDirectory(_storagePath);
            _logHandler.LogInfo($"Media Upload Service initialized with path: {_storagePath}");
        }
        catch (Exception ex)
        {
            _logHandler.LogError("An error occurred while trying to create data directory.", ex);
            throw;
        }
    }

    public async Task<Media> UploadFileAsync(IFormFile file)
    {
        _logHandler.LogInfo("Uploading a new file.");

        if (file == null)
        {
            _logHandler.LogError("File is null.", new ArgumentException("File is null", nameof(file)));
            throw new ArgumentException("File is null", nameof(file));
        }

        if (file.Length > MaxFileSize)
        {
            _logHandler.LogError("File is too large.", new ArgumentException("File is too large", nameof(file)));
            throw new ArgumentException("File is too large", nameof(file));
        }

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var fullPath = Path.Combine(_storagePath, fileName);

        try
        {
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            _logHandler.LogError("An error occurred while saving the file.", ex);
            throw;
        }

        var media = new Media
        {
            Name = fileName,
            Type = file.ContentType
        };

        _context.Media.Add(media);
        await _context.SaveChangesAsync();

        _logHandler.LogInfo("File uploaded and media record created successfully.");

        return media;
    }

    public async Task<(Stream fileStream, string contentType)> GetFileAsync(string fileName)
    {
        _logHandler.LogInfo($"Retrieving file: {fileName}");

        var fullPath = Path.Combine(_storagePath, fileName);
        if (!File.Exists(fullPath))
        {
            _logHandler.LogWarn($"File not found: {fileName}", new FileNotFoundException("File not found", fileName));
            throw new FileNotFoundException("File not found", fileName);
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        var contentType = GetContentType(fileName);

        _logHandler.LogInfo("File retrieved successfully.");

        return (fileStream, contentType);
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream",
        };
    }

    public void DeleteFile(string fileName)
    {
        _logHandler.LogInfo($"Deleting file: {fileName}");

        var fullPath = Path.Combine(_storagePath, fileName);
        if (!File.Exists(fullPath))
        {
            _logHandler.LogWarn($"File not found: {fileName}", new FileNotFoundException("File not found", fileName));
            throw new FileNotFoundException("File not found", fileName);
        }

        try
        {
            File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logHandler.LogError("An error occurred while trying to delete the file.", ex);
            throw;
        }

        var media = _context.Media.FirstOrDefault(m => m.Name == fileName);
        if (media != null)
        {
            _context.Media.Remove(media);
            _context.SaveChanges();
        }

        _logHandler.LogInfo("File deleted successfully.");
    }
}