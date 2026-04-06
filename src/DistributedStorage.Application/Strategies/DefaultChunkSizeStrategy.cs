using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Application.Strategies;

public class DefaultChunkSizeStrategy : IChunkSizeStrategy
{
    private readonly ILogger<DefaultChunkSizeStrategy> _logger;

    public DefaultChunkSizeStrategy(ILogger<DefaultChunkSizeStrategy> logger)
    {
        _logger = logger;
    }

    public int ResolveChunkSize(long fileSize)
    {
        const int KB = 1024;
        const int MB = 1024 * KB;

        var chunkSize = fileSize switch
        {
            < 10 * MB => 512 * KB,
            < 1L * 1024 * MB => 5 * MB,
            _ => 20 * MB
        };

        _logger.LogInformation("{@LogCategory} | Chunk boyutu belirlendi. Dosya boyutu: {FileSize} byte → Chunk boyutu: {ChunkSize} byte",
            LogCategory.Chunking, fileSize, chunkSize);

        return chunkSize;
    }
}
