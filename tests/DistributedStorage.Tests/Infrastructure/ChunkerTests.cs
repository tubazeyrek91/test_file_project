using DistributedStorage.Infrastructure.Chunking;
using Microsoft.Extensions.Logging;
using Moq;

namespace DistributedStorage.Tests.Infrastructure;

public class ChunkerTests : IDisposable
{
    private readonly Chunker _sut;
    private readonly string _testFilePath;

    public ChunkerTests()
    {
        _sut = new Chunker(Mock.Of<ILogger<Chunker>>());
        _testFilePath = Path.GetTempFileName();
    }

    [Fact]
    public void Split_SmallFile_ReturnsSingleChunk()
    {
        File.WriteAllBytes(_testFilePath, new byte[100]);

        var chunks = _sut.Split(_testFilePath, 1024).ToList();

        Assert.Single(chunks);
        Assert.Equal(100, chunks[0].Length);
    }

    [Fact]
    public void Split_ExactMultiple_ReturnsCorrectChunkCount()
    {
        File.WriteAllBytes(_testFilePath, new byte[1024]);

        var chunks = _sut.Split(_testFilePath, 256).ToList();

        Assert.Equal(4, chunks.Count);
        Assert.All(chunks, c => Assert.Equal(256, c.Length));
    }

    [Fact]
    public void Split_NotExactMultiple_LastChunkIsSmaller()
    {
        File.WriteAllBytes(_testFilePath, new byte[1000]);

        var chunks = _sut.Split(_testFilePath, 300).ToList();

        Assert.Equal(4, chunks.Count);
        Assert.Equal(300, chunks[0].Length);
        Assert.Equal(100, chunks[3].Length);
    }

    [Fact]
    public void Split_ReconstructedDataMatchesOriginal()
    {
        var original = new byte[1500];
        new Random(42).NextBytes(original);
        File.WriteAllBytes(_testFilePath, original);

        var chunks = _sut.Split(_testFilePath, 400).ToList();
        var reconstructed = chunks.SelectMany(c => c).ToArray();

        Assert.Equal(original, reconstructed);
    }

    [Fact]
    public void Split_EmptyFile_ReturnsNoChunks()
    {
        File.WriteAllBytes(_testFilePath, Array.Empty<byte>());

        var chunks = _sut.Split(_testFilePath, 1024).ToList();

        Assert.Empty(chunks);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }
}
