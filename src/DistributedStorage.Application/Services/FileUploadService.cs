using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using DistributedStorage.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Application.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IEnumerable<IStorageProvider> _storageProviders;
    private readonly IChunkSizeStrategy _chunkSizeStrategy;
    private readonly IChunker _chunker;
    private readonly IHashService _hashService;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        IEnumerable<IStorageProvider> storageProviders,
        IChunkSizeStrategy chunkSizeStrategy,
        IChunker chunker,
        IHashService hashService,
        IMetadataRepository metadataRepository,
        ILogger<FileUploadService> logger)
    {
        _storageProviders = storageProviders;
        _chunkSizeStrategy = chunkSizeStrategy;
        _chunker = chunker;
        _hashService = hashService;
        _metadataRepository = metadataRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> UploadAsync(IEnumerable<string> filePaths)
    {
        var uploadedFileIds = new List<Guid>();

        foreach (var path in filePaths)
        {
            var fileId = await UploadSingleFileAsync(path);
            uploadedFileIds.Add(fileId);
        }

        return uploadedFileIds;
    }

    private async Task<Guid> UploadSingleFileAsync(string filePath)
    {
        _logger.LogInformation("{@LogCategory} | Dosya yükleme başladı. Dosya: {File}",
            LogCategory.Upload, filePath);

        var fileInfo = new FileInfo(filePath);
        var chunkSize = _chunkSizeStrategy.ResolveChunkSize(fileInfo.Length);

        _logger.LogInformation("{@LogCategory} | Chunk boyutu belirlendi. Dosya boyutu: {FileSize} byte, Chunk boyutu: {ChunkSize} byte",
            LogCategory.Upload, fileInfo.Length, chunkSize);

        var chunks = _chunker.Split(filePath, chunkSize).ToList();

        var fileMetadata = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalFileName = fileInfo.Name
        };

        var providers = _storageProviders.ToList();

        for (int i = 0; i < chunks.Count; i++)
        {
            var provider = providers[i % providers.Count];
            var chunkKey = $"{fileMetadata.FileId}_{i}";

            _logger.LogInformation("{@LogCategory} | Chunk #{ChunkIndex} '{Provider}' provider'ına yazılıyor. Key: {ChunkKey}",
                LogCategory.Upload, i, provider.Name, chunkKey);

            await provider.SaveAsync(chunkKey, chunks[i]);

            fileMetadata.Chunks.Add(new ChunkMetadata
            {
                ChunkKey = chunkKey,
                Order = i,
                StorageProvider = provider.Name,
                Checksum = _hashService.Compute(chunks[i])
            });
        }

        fileMetadata.Checksum = _hashService.ComputeFile(filePath);

        await _metadataRepository.SaveAsync(fileMetadata);

        _logger.LogInformation("{@LogCategory} | Dosya yükleme tamamlandı. Dosya: {File}, FileId: {FileId}, Toplam chunk: {ChunkCount}",
            LogCategory.Upload, filePath, fileMetadata.FileId, chunks.Count);

        return fileMetadata.FileId;
    }
}
