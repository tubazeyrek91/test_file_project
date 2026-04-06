using DistributedStorage.Infrastructure.Hashing;
using Microsoft.Extensions.Logging;
using Moq;

namespace DistributedStorage.Tests.Infrastructure;

public class Sha256HashServiceTests : IDisposable
{
    private readonly Sha256HashService _sut;
    private readonly string _testFilePath;

    public Sha256HashServiceTests()
    {
        _sut = new Sha256HashService(Mock.Of<ILogger<Sha256HashService>>());
        _testFilePath = Path.GetTempFileName();
    }

    [Fact]
    public void Compute_ReturnsDeterministicHash()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var hash1 = _sut.Compute(data);
        var hash2 = _sut.Compute(data);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Compute_DifferentData_ReturnsDifferentHash()
    {
        var hash1 = _sut.Compute(new byte[] { 1, 2, 3 });
        var hash2 = _sut.Compute(new byte[] { 4, 5, 6 });

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Compute_ReturnsValidHexString()
    {
        var hash = _sut.Compute(new byte[] { 1, 2, 3 });

        Assert.Equal(64, hash.Length); // SHA256 = 32 bytes = 64 hex chars
        Assert.Matches("^[0-9A-F]{64}$", hash);
    }

    [Fact]
    public void ComputeFile_MatchesComputeWithSameData()
    {
        var data = new byte[] { 10, 20, 30, 40 };
        File.WriteAllBytes(_testFilePath, data);

        var fileHash = _sut.ComputeFile(_testFilePath);
        var dataHash = _sut.Compute(data);

        Assert.Equal(dataHash, fileHash);
    }

    [Fact]
    public void ComputeFile_DifferentFiles_ReturnDifferentHashes()
    {
        var file2 = Path.GetTempFileName();
        File.WriteAllBytes(_testFilePath, new byte[] { 1, 2, 3 });
        File.WriteAllBytes(file2, new byte[] { 4, 5, 6 });

        var hash1 = _sut.ComputeFile(_testFilePath);
        var hash2 = _sut.ComputeFile(file2);

        Assert.NotEqual(hash1, hash2);
        File.Delete(file2);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }
}
