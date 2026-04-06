namespace DistributedStorage.Application.Interfaces;

public interface IFileUploadService
{
    Task<IReadOnlyList<Guid>> UploadAsync(IEnumerable<string> filePaths);
}
