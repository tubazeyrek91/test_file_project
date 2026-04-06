using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using DistributedStorage.Domain.Entities;
using DistributedStorage.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Persistence.Repositories;

public class MetadataRepository : IMetadataRepository
{
    private readonly IDbContextFactory<MetadataDbContext> _contextFactory;
    private readonly ILogger<MetadataRepository> _logger;

    public MetadataRepository(IDbContextFactory<MetadataDbContext> contextFactory, ILogger<MetadataRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SaveAsync(FileMetadata metadata)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Files.Add(metadata);
        await context.SaveChangesAsync();
        _logger.LogInformation("{@LogCategory} | Metadata kaydedildi. FileId: {FileId}, Dosya: {FileName}, Chunk sayısı: {ChunkCount}",
            LogCategory.Metadata, metadata.FileId, metadata.OriginalFileName, metadata.Chunks.Count);
    }

    public async Task<FileMetadata> GetAsync(Guid fileId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var result = await context.Files
            .Include(f => f.Chunks)
            .FirstAsync(f => f.FileId == fileId);
        _logger.LogInformation("{@LogCategory} | Metadata okundu. FileId: {FileId}, Dosya: {FileName}",
            LogCategory.Metadata, fileId, result.OriginalFileName);
        return result;
    }

    public async Task<IReadOnlyList<FileMetadata>> GetAllAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var results = await context.Files
            .AsNoTracking()
            .Include(f => f.Chunks)
            .OrderByDescending(f => f.FileId)
            .ToListAsync();
        _logger.LogInformation("{@LogCategory} | Tüm metadata listelendi. Toplam dosya: {Count}",
            LogCategory.Metadata, results.Count);
        return results;
    }
}
