namespace DistributedStorage.Application.Interfaces;

public interface IHashService
{
    string Compute(byte[] data);
    string ComputeFile(string path);
}
