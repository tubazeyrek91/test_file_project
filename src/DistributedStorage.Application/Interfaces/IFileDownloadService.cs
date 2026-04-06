namespace DistributedStorage.Application.Interfaces;

public interface IFileDownloadService
{
    Task DownloadAsync(Guid fileId, string targetPath);
}
