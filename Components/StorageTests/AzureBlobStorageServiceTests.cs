using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Storage.Configuration;
using Storage.Models;
using Storage.Services;
using Xunit;

namespace StorageTests;

public class AzureBlobStorageServiceTests
{
    private readonly Mock<IOptions<StorageOptions>> _mockOptions;
    private readonly Mock<ILogger<AzureBlobStorageService>> _mockLogger;
    private readonly StorageOptions _storageOptions;

    public AzureBlobStorageServiceTests()
    {
        _mockOptions = new Mock<IOptions<StorageOptions>>();
        _mockLogger = new Mock<ILogger<AzureBlobStorageService>>();
        
        _storageOptions = new StorageOptions
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-screenshots",
            ThumbnailContainer = "test-thumbnails",
            DefaultOptimization = new ImageOptimizationSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1080,
                JpegQuality = 85,
                GenerateThumbnail = true,
                ThumbnailSize = new System.Drawing.Size(300, 200)
            }
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_storageOptions);
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesInstance()
    {
        var service = new AzureBlobStorageService(_mockOptions.Object, _mockLogger.Object);
        
        Assert.NotNull(service);
    }

    [Fact]
    public async Task OptimizeImageAsync_ValidImage_ReturnsOptimizedResult()
    {
        var service = new AzureBlobStorageService(_mockOptions.Object, _mockLogger.Object);
        var testImageData = CreateTestImageData();
        var settings = new ImageOptimizationSettings();

        var result = await service.OptimizeImageAsync(testImageData, settings);

        Assert.NotNull(result);
        Assert.True(result.OptimizedImageData.Length > 0);
        Assert.True(result.ThumbnailData.Length > 0);
        Assert.True(result.OriginalSize > 0);
        Assert.True(result.OptimizedSize > 0);
        Assert.True(result.CompressionRatio > 0);
    }

    [Fact]
    public async Task OptimizeImageAsync_InvalidImageData_ThrowsException()
    {
        var service = new AzureBlobStorageService(_mockOptions.Object, _mockLogger.Object);
        var invalidImageData = new byte[] { 1, 2, 3, 4, 5 };
        var settings = new ImageOptimizationSettings();

        await Assert.ThrowsAsync<Storage.Exceptions.StorageException>(
            () => service.OptimizeImageAsync(invalidImageData, settings));
    }

    [Theory]
    [InlineData(1920, 1080, 85)]
    [InlineData(800, 600, 75)]
    [InlineData(1280, 720, 90)]
    public async Task OptimizeImageAsync_DifferentSettings_OptimizesCorrectly(int maxWidth, int maxHeight, int quality)
    {
        var service = new AzureBlobStorageService(_mockOptions.Object, _mockLogger.Object);
        var testImageData = CreateTestImageData();
        var settings = new ImageOptimizationSettings
        {
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
            JpegQuality = quality
        };

        var result = await service.OptimizeImageAsync(testImageData, settings);

        Assert.NotNull(result);
        Assert.True(result.OptimizedImageData.Length > 0);
        Assert.True(result.ThumbnailData.Length > 0);
    }

    [Fact]
    public void ImageOptimizationSettings_DefaultValues_AreCorrect()
    {
        var settings = new ImageOptimizationSettings();

        Assert.Equal(1920, settings.MaxWidth);
        Assert.Equal(1080, settings.MaxHeight);
        Assert.Equal(85, settings.JpegQuality);
        Assert.True(settings.GenerateThumbnail);
        Assert.Equal(new System.Drawing.Size(300, 200), settings.ThumbnailSize);
    }

    [Fact]
    public void UploadResult_DefaultValues_AreCorrect()
    {
        var result = new UploadResult();

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.BlobName);
        Assert.Equal(string.Empty, result.ThumbnailBlobName);
        Assert.Equal(0, result.FileSizeBytes);
        Assert.Equal(TimeSpan.Zero, result.ProcessingTime);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OptimizedImageResult_DefaultValues_AreCorrect()
    {
        var result = new OptimizedImageResult();

        Assert.Empty(result.OptimizedImageData);
        Assert.Empty(result.ThumbnailData);
        Assert.Equal(0, result.OriginalSize);
        Assert.Equal(0, result.OptimizedSize);
        Assert.Equal(0, result.CompressionRatio);
    }

    [Fact]
    public void StorageOptions_DefaultValues_AreCorrect()
    {
        var options = new StorageOptions();

        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal("screenshots", options.ContainerName);
        Assert.Equal("thumbnails", options.ThumbnailContainer);
        Assert.NotNull(options.DefaultOptimization);
    }

    private static byte[] CreateTestImageData()
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
        image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.Blue));
        
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }
}