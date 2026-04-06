using System.Security.Cryptography;
using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Infrastructure.Hashing;

public class Sha256HashService : IHashService
{
    private readonly ILogger<Sha256HashService> _logger;

    public Sha256HashService(ILogger<Sha256HashService> logger)
    {
        _logger = logger;
    }

    public string Compute(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(data));
        _logger.LogDebug("{@LogCategory} | Chunk hash hesaplandı. Boyut: {Size} byte, Hash: {Hash}",
            LogCategory.Hashing, data.Length, hash);
        return hash;
    }

    public string ComputeFile(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = Convert.ToHexString(sha.ComputeHash(fs));
        _logger.LogInformation("{@LogCategory} | Dosya hash hesaplandı. Dosya: {FilePath}, Hash: {Hash}",
            LogCategory.Hashing, path, hash);
        return hash;
    }
}
