using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using DistributedStorage.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Application.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly IEnumerable<IStorageProvider> _storageProviders;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IHashService _hashService;
    private readonly ILogger<FileDownloadService> _logger;

    public FileDownloadService(
        IEnumerable<IStorageProvider> storageProviders,
        IMetadataRepository metadataRepository,
        IHashService hashService,
        ILogger<FileDownloadService> logger)
    {
        _storageProviders = storageProviders;
        _metadataRepository = metadataRepository;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task DownloadAsync(Guid fileId, string targetPath)
    {
        _logger.LogInformation("{@LogCategory} | Dosya indirme başladı. FileId: {FileId}, Hedef: {TargetPath}",
            LogCategory.Download, fileId, targetPath);

        var metadata = await _metadataRepository.GetAsync(fileId);

        _logger.LogInformation("{@LogCategory} | Metadata alındı. Dosya: {FileName}, Chunk sayısı: {ChunkCount}",
            LogCategory.Download, metadata.OriginalFileName, metadata.Chunks.Count);

        using (var output = new FileStream(
            targetPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            foreach (var chunk in metadata.Chunks.OrderBy(c => c.Order))
            {
                var provider = _storageProviders
                    .First(p => p.Name == chunk.StorageProvider);

                _logger.LogInformation("{@LogCategory} | Chunk #{Order} '{Provider}' provider'ından okunuyor. Key: {ChunkKey}",
                    LogCategory.Download, chunk.Order, provider.Name, chunk.ChunkKey);

                var data = await provider.ReadAsync(chunk.ChunkKey);
                await output.WriteAsync(data);
            }
        }

        var reconstructedHash = _hashService.ComputeFile(targetPath);

        if (reconstructedHash != metadata.Checksum)
        {
            _logger.LogError("{@LogCategory} | Dosya bütünlük hatası! FileId: {FileId}, Beklenen: {Expected}, Hesaplanan: {Actual}",
                LogCategory.Download, fileId, metadata.Checksum, reconstructedHash);
            throw new IntegrityException("Dosya bütünlük hatası.");
        }

        _logger.LogInformation("{@LogCategory} | Dosya indirme tamamlandı. FileId: {FileId}, Hedef: {TargetPath}",
            LogCategory.Download, fileId, targetPath);
    }
}
