namespace DistributedStorage.Domain.Entities;

public class ChunkMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ChunkKey { get; set; } = string.Empty;
    public int Order { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}
