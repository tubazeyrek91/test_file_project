namespace DistributedStorage.Application.Interfaces;

public interface IChunker
{
    IEnumerable<byte[]> Split(string filePath, int chunkSize);
}
