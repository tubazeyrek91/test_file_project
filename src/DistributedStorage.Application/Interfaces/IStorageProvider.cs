namespace DistributedStorage.Application.Interfaces;

public interface IStorageProvider
{
    string Name { get; }
    Task SaveAsync(string key, byte[] data);
    Task<byte[]> ReadAsync(string key);
}
