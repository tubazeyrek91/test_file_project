using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Services;
using DistributedStorage.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace DistributedStorage.Tests.Services;

public class FileUploadServiceTests : IDisposable
{
    private readonly Mock<IStorageProvider> _fsProvider;
    private readonly Mock<IStorageProvider> _dbProvider;
    private readonly Mock<IChunkSizeStrategy> _chunkStrategy;
    private readonly Mock<IChunker> _chunker;
    private readonly Mock<IHashService> _hashService;
    private readonly Mock<IMetadataRepository> _metadataRepo;
    private readonly FileUploadService _sut;
    private readonly string _testFilePath;

    public FileUploadServiceTests()
    {
        _fsProvider = new Mock<IStorageProvider>();
        _fsProvider.Setup(p => p.Name).Returns("FileSystem");

        _dbProvider = new Mock<IStorageProvider>();
        _dbProvider.Setup(p => p.Name).Returns("Database");

        _chunkStrategy = new Mock<IChunkSizeStrategy>();
        _chunker = new Mock<IChunker>();
        _hashService = new Mock<IHashService>();
        _metadataRepo = new Mock<IMetadataRepository>();

        _sut = new FileUploadService(
            new[] { _fsProvider.Object, _dbProvider.Object },
            _chunkStrategy.Object,
            _chunker.Object,
            _hashService.Object,
            _metadataRepo.Object,
            Mock.Of<ILogger<FileUploadService>>());

        _testFilePath = Path.GetTempFileName();
        File.WriteAllBytes(_testFilePath, new byte[1024]);
    }

    [Fact]
    public async Task UploadAsync_SingleFile_ReturnsOneFileId()
    {
        SetupDefaults(chunkCount: 2);

        var result = await _sut.UploadAsync(new[] { _testFilePath });

        Assert.Single(result);
        Assert.NotEqual(Guid.Empty, result[0]);
    }

    [Fact]
    public async Task UploadAsync_MultipleFiles_ReturnsCorrectCount()
    {
        SetupDefaults(chunkCount: 1);

        var file2 = Path.GetTempFileName();
        File.WriteAllBytes(file2, new byte[512]);

        var result = await _sut.UploadAsync(new[] { _testFilePath, file2 });

        Assert.Equal(2, result.Count);
        File.Delete(file2);
    }

    [Fact]
    public async Task UploadAsync_ChunksDistributedRoundRobin()
    {
        SetupDefaults(chunkCount: 4);

        await _sut.UploadAsync(new[] { _testFilePath });

        // 4 chunk, 2 provider → her biri 2 kez çağrılmalı
        _fsProvider.Verify(p => p.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(2));
        _dbProvider.Verify(p => p.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UploadAsync_SavesMetadataWithCorrectChunkCount()
    {
        SetupDefaults(chunkCount: 3);

        await _sut.UploadAsync(new[] { _testFilePath });

        _metadataRepo.Verify(r => r.SaveAsync(It.Is<FileMetadata>(
            m => m.Chunks.Count == 3 && !string.IsNullOrEmpty(m.Checksum))), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ComputesFileChecksum()
    {
        SetupDefaults(chunkCount: 1);

        await _sut.UploadAsync(new[] { _testFilePath });

        _hashService.Verify(h => h.ComputeFile(_testFilePath), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_ComputesChunkChecksums()
    {
        SetupDefaults(chunkCount: 3);

        await _sut.UploadAsync(new[] { _testFilePath });

        _hashService.Verify(h => h.Compute(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Fact]
    public async Task UploadAsync_UsesChunkSizeStrategy()
    {
        SetupDefaults(chunkCount: 1);

        await _sut.UploadAsync(new[] { _testFilePath });

        _chunkStrategy.Verify(s => s.ResolveChunkSize(It.IsAny<long>()), Times.Once);
    }

    private void SetupDefaults(int chunkCount)
    {
        _chunkStrategy.Setup(s => s.ResolveChunkSize(It.IsAny<long>())).Returns(512);

        var chunks = Enumerable.Range(0, chunkCount)
            .Select(_ => new byte[] { 1, 2, 3 })
            .ToList();
        _chunker.Setup(c => c.Split(It.IsAny<string>(), It.IsAny<int>())).Returns(chunks);

        _hashService.Setup(h => h.Compute(It.IsAny<byte[]>())).Returns("CHUNK_HASH");
        _hashService.Setup(h => h.ComputeFile(It.IsAny<string>())).Returns("FILE_HASH");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }
}
