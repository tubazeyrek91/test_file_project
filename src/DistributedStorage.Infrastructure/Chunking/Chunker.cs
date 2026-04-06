using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Infrastructure.Chunking;

public class Chunker : IChunker
{
    private readonly ILogger<Chunker> _logger;

    public Chunker(ILogger<Chunker> logger)
    {
        _logger = logger;
    }

    public IEnumerable<byte[]> Split(string filePath, int chunkSize)
    {
        _logger.LogInformation("{@LogCategory} | Dosya parçalanıyor. Dosya: {FilePath}, ChunkSize: {ChunkSize}",
            LogCategory.Chunking, filePath, chunkSize);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var buffer = new byte[chunkSize];
        int chunkIndex = 0;

        int read;
        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            _logger.LogDebug("{@LogCategory} | Chunk #{ChunkIndex} oluşturuldu. Boyut: {Size} byte",
                LogCategory.Chunking, chunkIndex, read);
            chunkIndex++;
            yield return buffer[..read].ToArray();
        }

        _logger.LogInformation("{@LogCategory} | Dosya parçalama tamamlandı. Toplam chunk: {TotalChunks}",
            LogCategory.Chunking, chunkIndex);
    }
}
