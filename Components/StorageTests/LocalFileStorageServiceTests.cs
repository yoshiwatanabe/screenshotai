using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Storage.Configuration;
using Storage.Models;
using Storage.Services;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace StorageTests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly Mock<IOptions<StorageOptions>> _mockOptions;
    private readonly Mock<ILogger<LocalFileStorageService>> _mockLogger;
    private readonly StorageOptions _storageOptions;
    private readonly LocalFileStorageService _service;
    private readonly string _testDirectory;
    private readonly List<string> _createdFiles = [];

    public LocalFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "StorageTests", Guid.NewGuid().ToString());
        
        _mockOptions = new Mock<IOptions<StorageOptions>>();
        _mockLogger = new Mock<ILogger<LocalFileStorageService>>();
        
        _storageOptions = new StorageOptions
        {
            ScreenshotsDirectory = _testDirectory,
            ThumbnailsDirectory = Path.Combine(_testDirectory, "Thumbnails"),
            CreateDirectoriesIfNotExist = true,
            DefaultOptimization = new ImageOptimizationSettings
            {
                MaxWidth = 1920,
                MaxHeight = 1080,
                JpegQuality = 85
            }
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_storageOptions);
        _service = new LocalFileStorageService(_mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesInstance()
    {
        Assert.NotNull(_service);
        Assert.True(Directory.Exists(_storageOptions.ScreenshotsDirectory));
        Assert.True(Directory.Exists(_storageOptions.ThumbnailsDirectory));
    }

    [Fact]
    public async Task UploadFromClipboardAsync_ValidImage_SavesSuccessfully()
    {
        // Arrange
        var testImageData = CreateTestImageData();
        var fileName = "test_clipboard.jpg";

        // Act
        var result = await _service.UploadFromClipboardAsync(testImageData, fileName);

        // Assert
        Assert.True(result.Success, $"Upload failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.BlobName);
        Assert.NotEmpty(result.ThumbnailBlobName);
        Assert.True(result.FileSizeBytes > 0);
        Assert.True(result.ProcessingTime > TimeSpan.Zero);

        // Track for cleanup
        _createdFiles.Add(result.BlobName);

        // Verify files exist
        var imagePath = await _service.GetImagePathAsync(result.BlobName);
        var thumbnailPath = await _service.GetThumbnailPathAsync(result.BlobName);
        
        Assert.True(File.Exists(imagePath));
        Assert.True(File.Exists(thumbnailPath));
    }

    [Fact]
    public async Task OptimizeImageAsync_ValidImage_ReturnsOptimizedResult()
    {
        // Arrange
        var testImageData = CreateTestImageData();
        var settings = new ImageOptimizationSettings
        {
            MaxWidth = 800,
            MaxHeight = 600,
            JpegQuality = 75
        };

        // Act
        var result = await _service.OptimizeImageAsync(testImageData, settings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimizedImageData.Length > 0);
        Assert.True(result.ThumbnailData.Length > 0);
        Assert.True(result.OriginalSize > 0);
        Assert.True(result.OptimizedSize > 0);
        Assert.True(result.CompressionRatio > 0);
    }

    [Fact]
    public async Task IsHealthyAsync_ValidConfiguration_ReturnsTrue()
    {
        // Act
        var isHealthy = await _service.IsHealthyAsync();

        // Assert
        Assert.True(isHealthy);
    }

    [Fact]
    public void ImageOptimizationSettings_DefaultValues_AreCorrect()
    {
        var settings = new ImageOptimizationSettings();

        Assert.Equal(1920, settings.MaxWidth);
        Assert.Equal(1080, settings.MaxHeight);
        Assert.Equal(85, settings.JpegQuality);
        Assert.True(settings.GenerateThumbnail);
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
    public void StorageOptions_DefaultValues_AreCorrect()
    {
        var options = new StorageOptions();

        Assert.NotNull(options.ScreenshotsDirectory);
        Assert.NotNull(options.ThumbnailsDirectory);
        Assert.True(options.CreateDirectoriesIfNotExist);
        Assert.NotNull(options.DefaultOptimization);
        Assert.True(options.MaxFileSizeBytes > 0);
    }

    private static byte[] CreateTestImageData()
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
        image.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.Blue));
        
        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    public void Dispose()
    {
        // Clean up any created files
        foreach (var fileName in _createdFiles)
        {
            try
            {
                _service.DeleteImageAsync(fileName).Wait();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up test directory
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}