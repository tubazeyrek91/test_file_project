namespace DistributedStorage.Application.Interfaces;

public interface IChunkSizeStrategy
{
    int ResolveChunkSize(long fileSize);
}
