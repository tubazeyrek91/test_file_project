using DistributedStorage.Application.Strategies;
using Microsoft.Extensions.Logging;
using Moq;

namespace DistributedStorage.Tests.Strategies;

public class DefaultChunkSizeStrategyTests
{
    private readonly DefaultChunkSizeStrategy _sut = new(Mock.Of<ILogger<DefaultChunkSizeStrategy>>());

    [Theory]
    [InlineData(100, 512 * 1024)]             // 100 byte → 512 KB
    [InlineData(5 * 1024 * 1024, 512 * 1024)] // 5 MB → 512 KB
    [InlineData(9_999_999, 512 * 1024)]        // ~10 MB → 512 KB
    public void ResolveChunkSize_Under10MB_Returns512KB(long fileSize, int expected)
    {
        Assert.Equal(expected, _sut.ResolveChunkSize(fileSize));
    }

    [Theory]
    [InlineData(10 * 1024 * 1024, 5 * 1024 * 1024)]       // 10 MB → 5 MB
    [InlineData(500L * 1024 * 1024, 5 * 1024 * 1024)]      // 500 MB → 5 MB
    public void ResolveChunkSize_10MBto1GB_Returns5MB(long fileSize, int expected)
    {
        Assert.Equal(expected, _sut.ResolveChunkSize(fileSize));
    }

    [Theory]
    [InlineData(1L * 1024 * 1024 * 1024, 20 * 1024 * 1024)]   // 1 GB → 20 MB
    [InlineData(5L * 1024 * 1024 * 1024, 20 * 1024 * 1024)]   // 5 GB → 20 MB
    public void ResolveChunkSize_Over1GB_Returns20MB(long fileSize, int expected)
    {
        Assert.Equal(expected, _sut.ResolveChunkSize(fileSize));
    }
}
