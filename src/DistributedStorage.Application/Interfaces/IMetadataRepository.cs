using DistributedStorage.Domain.Entities;

namespace DistributedStorage.Application.Interfaces;

public interface IMetadataRepository
{
    Task SaveAsync(FileMetadata metadata);
    Task<FileMetadata> GetAsync(Guid fileId);
    Task<IReadOnlyList<FileMetadata>> GetAllAsync();
}
