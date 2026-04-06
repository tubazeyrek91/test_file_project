namespace DistributedStorage.Domain.Entities;

public class FileMetadata
{
    public Guid FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;

    public ICollection<ChunkMetadata> Chunks { get; set; } = new List<ChunkMetadata>();
}
