using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Infrastructure.Storage;

public class FileSystemStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<FileSystemStorageProvider> _logger;

    public FileSystemStorageProvider(ILogger<FileSystemStorageProvider> logger, string basePath = "storage/fs")
    {
        _basePath = basePath;
        _logger = logger;
    }

    public string Name => "FileSystem";

    public async Task SaveAsync(string key, byte[] data)
    {
        Directory.CreateDirectory(_basePath);
        var filePath = Path.Combine(_basePath, key);
        await File.WriteAllBytesAsync(filePath, data);
        _logger.LogInformation("{@LogCategory} | Chunk dosya sistemine yazıldı. Key: {Key}, Boyut: {Size} byte, Yol: {Path}",
            LogCategory.Storage, key, data.Length, filePath);
    }

    public async Task<byte[]> ReadAsync(string key)
    {
        var filePath = Path.Combine(_basePath, key);
        var data = await File.ReadAllBytesAsync(filePath);
        _logger.LogInformation("{@LogCategory} | Chunk dosya sisteminden okundu. Key: {Key}, Boyut: {Size} byte",
            LogCategory.Storage, key, data.Length);
        return data;
    }
}
