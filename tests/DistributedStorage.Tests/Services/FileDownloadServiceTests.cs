using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Services;
using DistributedStorage.Domain.Entities;
using DistributedStorage.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DistributedStorage.Tests.Services;

public class FileDownloadServiceTests : IDisposable
{
    private readonly Mock<IStorageProvider> _fsProvider;
    private readonly Mock<IStorageProvider> _dbProvider;
    private readonly Mock<IMetadataRepository> _metadataRepo;
    private readonly Mock<IHashService> _hashService;
    private readonly FileDownloadService _sut;
    private readonly string _targetPath;

    public FileDownloadServiceTests()
    {
        _fsProvider = new Mock<IStorageProvider>();
        _fsProvider.Setup(p => p.Name).Returns("FileSystem");

        _dbProvider = new Mock<IStorageProvider>();
        _dbProvider.Setup(p => p.Name).Returns("Database");

        _metadataRepo = new Mock<IMetadataRepository>();
        _hashService = new Mock<IHashService>();

        _sut = new FileDownloadService(
            new[] { _fsProvider.Object, _dbProvider.Object },
            _metadataRepo.Object,
            _hashService.Object,
            Mock.Of<ILogger<FileDownloadService>>());

        _targetPath = Path.Combine(Path.GetTempPath(), $"test_download_{Guid.NewGuid()}.bin");
    }

    [Fact]
    public async Task DownloadAsync_ReconstructsFileFromChunks()
    {
        var fileId = Guid.NewGuid();
        var chunk0 = new byte[] { 1, 2, 3 };
        var chunk1 = new byte[] { 4, 5, 6 };

        SetupMetadata(fileId, ("FileSystem", chunk0), ("Database", chunk1));
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("VALID_HASH");

        await _sut.DownloadAsync(fileId, _targetPath);

        var result = await File.ReadAllBytesAsync(_targetPath);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, result);
    }

    [Fact]
    public async Task DownloadAsync_ReadsChunksInOrder()
    {
        var fileId = Guid.NewGuid();
        SetupMetadata(fileId,
            ("Database", new byte[] { 10 }),
            ("FileSystem", new byte[] { 20 }),
            ("Database", new byte[] { 30 }));
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("VALID_HASH");

        await _sut.DownloadAsync(fileId, _targetPath);

        var result = await File.ReadAllBytesAsync(_targetPath);
        Assert.Equal(new byte[] { 10, 20, 30 }, result);
    }

    [Fact]
    public async Task DownloadAsync_ThrowsIntegrityException_WhenChecksumMismatch()
    {
        var fileId = Guid.NewGuid();
        SetupMetadata(fileId, ("FileSystem", new byte[] { 1 }));
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("WRONG_HASH");

        await Assert.ThrowsAsync<IntegrityException>(
            () => _sut.DownloadAsync(fileId, _targetPath));
    }

    [Fact]
    public async Task DownloadAsync_PassesIntegrityCheck_WhenChecksumMatches()
    {
        var fileId = Guid.NewGuid();
        SetupMetadata(fileId, ("FileSystem", new byte[] { 1, 2 }));
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("VALID_HASH");

        var exception = await Record.ExceptionAsync(
            () => _sut.DownloadAsync(fileId, _targetPath));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DownloadAsync_UsesCorrectProviderPerChunk()
    {
        var fileId = Guid.NewGuid();
        SetupMetadata(fileId,
            ("FileSystem", new byte[] { 1 }),
            ("Database", new byte[] { 2 }));
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("VALID_HASH");

        await _sut.DownloadAsync(fileId, _targetPath);

        _fsProvider.Verify(p => p.ReadAsync(It.IsAny<string>()), Times.Once);
        _dbProvider.Verify(p => p.ReadAsync(It.IsAny<string>()), Times.Once);
    }

    private void SetupMetadata(Guid fileId, params (string provider, byte[] data)[] chunks)
    {
        var metadata = new FileMetadata
        {
            FileId = fileId,
            OriginalFileName = "test.bin",
            Checksum = "VALID_HASH"
        };

        for (int i = 0; i < chunks.Length; i++)
        {
            var chunkKey = $"{fileId}_{i}";
            metadata.Chunks.Add(new ChunkMetadata
            {
                ChunkKey = chunkKey,
                Order = i,
                StorageProvider = chunks[i].provider,
                Checksum = "CHUNK_HASH"
            });

            var provider = chunks[i].provider == "FileSystem" ? _fsProvider : _dbProvider;
            provider.Setup(p => p.ReadAsync(chunkKey)).ReturnsAsync(chunks[i].data);
        }

        _metadataRepo.Setup(r => r.GetAsync(fileId)).ReturnsAsync(metadata);
    }

    public void Dispose()
    {
        if (File.Exists(_targetPath))
            File.Delete(_targetPath);
    }
}
