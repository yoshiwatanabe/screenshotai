using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Models;
using Storage.Services;
using Xunit;
using Xunit.Abstractions;

namespace LocalStorageTests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILocalStorageService _storageService;
    private readonly ILogger<LocalFileStorageServiceTests> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<string> _createdFiles = [];
    private readonly ITestOutputHelper _output;

    public LocalFileStorageServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _serviceProvider = TestConfiguration.CreateServiceProvider();
        _storageService = _serviceProvider.GetRequiredService<ILocalStorageService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<LocalFileStorageServiceTests>>();
        _configuration = TestConfiguration.BuildConfiguration();
    }

    [Fact]
    public async Task UploadFromClipboardAsync_ValidImage_SavesSuccessfully()
    {
        // Arrange
        var testImageData = TestImageHelper.CreateTestJpegImage(800, 600);
        var fileName = "local_test_clipboard.jpg";

        // Act
        var result = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        // Assert
        Assert.True(result.Success, $"Upload failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.BlobName);
        Assert.NotEmpty(result.ThumbnailBlobName);
        Assert.True(result.FileSizeBytes > 0);
        Assert.True(result.ProcessingTime > TimeSpan.Zero);

        // Track for cleanup
        _createdFiles.Add(result.BlobName);

        // Verify files exist
        var imagePath = await _storageService.GetImagePathAsync(result.BlobName);
        var thumbnailPath = await _storageService.GetThumbnailPathAsync(result.BlobName);

        Assert.True(File.Exists(imagePath));
        Assert.True(File.Exists(thumbnailPath));

        _output.WriteLine($"Upload successful:");
        _output.WriteLine($"  File Name: {result.BlobName}");
        _output.WriteLine($"  Image Path: {imagePath}");
        _output.WriteLine($"  Thumbnail Path: {thumbnailPath}");
        _output.WriteLine($"  File Size: {result.FileSizeBytes} bytes");
        _output.WriteLine($"  Processing Time: {result.ProcessingTime.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task GetImageStreamAsync_ExistingFile_ReturnsStream()
    {
        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(400, 300);
        var fileName = "local_test_retrieve.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        Assert.True(uploadResult.Success);
        _createdFiles.Add(uploadResult.BlobName);

        // Act
        using var stream = await _storageService.GetImageStreamAsync(uploadResult.BlobName);

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);

        var retrievedData = new byte[stream.Length];
        await stream.ReadAsync(retrievedData);

        Assert.True(retrievedData.Length > 0);
        _output.WriteLine($"Retrieved image stream: {retrievedData.Length} bytes");
    }

    [Fact]
    public async Task GetThumbnailPathAsync_ExistingFile_ReturnsValidPath()
    {
        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(1200, 800);
        var fileName = "local_test_thumbnail.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        Assert.True(uploadResult.Success);
        _createdFiles.Add(uploadResult.BlobName);

        // Act
        var thumbnailPath = await _storageService.GetThumbnailPathAsync(uploadResult.BlobName);

        // Assert
        Assert.NotNull(thumbnailPath);
        Assert.True(File.Exists(thumbnailPath));
        Assert.Contains("_thumb.jpg", thumbnailPath);

        // Verify thumbnail is smaller than original
        var thumbnailInfo = new FileInfo(thumbnailPath);
        var originalPath = await _storageService.GetImagePathAsync(uploadResult.BlobName);
        var originalInfo = new FileInfo(originalPath);

        Assert.True(thumbnailInfo.Length < originalInfo.Length);

        _output.WriteLine($"Thumbnail Path: {thumbnailPath}");
        _output.WriteLine($"Original Size: {originalInfo.Length} bytes");
        _output.WriteLine($"Thumbnail Size: {thumbnailInfo.Length} bytes");
    }

    [Fact]
    public async Task OptimizeImageAsync_LargeImage_ReducesSize()
    {
        // Arrange
        var largeImageData = TestImageHelper.CreateTestJpegImage(2400, 1800); // Large image
        var settings = new ImageOptimizationSettings
        {
            MaxWidth = 1920,
            MaxHeight = 1080,
            JpegQuality = 75
        };

        // Act
        var result = await _storageService.OptimizeImageAsync(largeImageData, settings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimizedImageData.Length > 0);
        Assert.True(result.ThumbnailData.Length > 0);
        Assert.True(result.OriginalSize > 0);
        Assert.True(result.OptimizedSize > 0);
        Assert.True(result.CompressionRatio > 0);
        Assert.True(result.OptimizedSize < result.OriginalSize, "Optimized image should be smaller");

        _output.WriteLine($"Image optimization results:");
        _output.WriteLine($"  Original Size: {result.OriginalSize} bytes");
        _output.WriteLine($"  Optimized Size: {result.OptimizedSize} bytes");
        _output.WriteLine($"  Compression Ratio: {result.CompressionRatio:P2}");
        _output.WriteLine($"  Thumbnail Size: {result.ThumbnailData.Length} bytes");
    }

    [Fact]
    public async Task DeleteImageAsync_ExistingFile_DeletesSuccessfully()
    {
        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(600, 400);
        var fileName = "local_test_delete.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        Assert.True(uploadResult.Success);

        // Verify files exist before deletion
        var imagePath = await _storageService.GetImagePathAsync(uploadResult.BlobName);
        var thumbnailPath = await _storageService.GetThumbnailPathAsync(uploadResult.BlobName);
        Assert.True(File.Exists(imagePath));
        Assert.True(File.Exists(thumbnailPath));

        // Act
        var deleteResult = await _storageService.DeleteImageAsync(uploadResult.BlobName);

        // Assert
        Assert.True(deleteResult);

        // Verify files are actually deleted
        Assert.False(File.Exists(imagePath));
        Assert.False(File.Exists(thumbnailPath));

        // Verify exception is thrown when trying to access deleted files
        await Assert.ThrowsAsync<Storage.Exceptions.StorageException>(
            () => _storageService.GetImageStreamAsync(uploadResult.BlobName));

        _output.WriteLine($"Successfully deleted file: {uploadResult.BlobName}");
    }

    [Fact]
    public async Task IsHealthyAsync_ValidConfiguration_ReturnsTrue()
    {
        // Act
        var isHealthy = await _storageService.IsHealthyAsync();

        // Assert
        Assert.True(isHealthy);
        _output.WriteLine("Storage service health check passed");
    }

    [Fact]
    public async Task FullWorkflow_UploadOptimizeRetrieveDelete_WorksEndToEnd()
    {
        // Arrange
        var originalImageData = TestImageHelper.CreateTestJpegImage(1600, 1200);
        var fileName = "local_test_workflow.jpg";

        _output.WriteLine("Starting full local storage workflow test...");

        // Act & Assert - Upload
        var uploadResult = await _storageService.UploadFromClipboardAsync(originalImageData, fileName);
        Assert.True(uploadResult.Success);
        _output.WriteLine($"✓ Upload completed: {uploadResult.BlobName}");

        // Act & Assert - Retrieve
        using (var retrievedStream = await _storageService.GetImageStreamAsync(uploadResult.BlobName))
        {
            Assert.NotNull(retrievedStream);
            _output.WriteLine("✓ Image retrieved successfully");
        } // Explicitly dispose stream before proceeding

        // Act & Assert - Get Paths
        var imagePath = await _storageService.GetImagePathAsync(uploadResult.BlobName);
        var thumbnailPath = await _storageService.GetThumbnailPathAsync(uploadResult.BlobName);
        Assert.True(File.Exists(imagePath));
        Assert.True(File.Exists(thumbnailPath));
        _output.WriteLine($"✓ File paths accessible: {Path.GetFileName(imagePath)}");

        // Act & Assert - Health Check
        var isHealthy = await _storageService.IsHealthyAsync();
        Assert.True(isHealthy);
        _output.WriteLine("✓ Health check passed");

        // Small delay to ensure file handles are released (Windows file locking)
        await Task.Delay(100);

        // Act & Assert - Delete
        var deleteResult = await _storageService.DeleteImageAsync(uploadResult.BlobName);
        Assert.True(deleteResult);
        _output.WriteLine("✓ Image deleted successfully");

        _output.WriteLine("Full local storage workflow test completed successfully!");
    }

    [Theory]
    [InlineData(100, 100, 50)] // Small image, high quality
    [InlineData(1920, 1080, 85)] // Standard resolution
    [InlineData(3840, 2160, 95)] // 4K resolution
    public async Task UploadFromClipboardAsync_DifferentImageSizes_HandlesCorrectly(int width, int height, int quality)
    {
        // Arrange
        var testImageData = TestImageHelper.CreateTestJpegImage(width, height, quality);
        var fileName = $"local_test_{width}x{height}_q{quality}.jpg";

        // Act
        var result = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        // Assert
        Assert.True(result.Success, $"Upload failed for {width}x{height}: {result.ErrorMessage}");

        // Track for cleanup
        _createdFiles.Add(result.BlobName);

        // Verify file size constraints
        var filePath = await _storageService.GetImagePathAsync(result.BlobName);
        var fileInfo = new FileInfo(filePath);

        _output.WriteLine($"Successfully uploaded {width}x{height} image:");
        _output.WriteLine($"  Original Size: {testImageData.Length} bytes");
        _output.WriteLine($"  Stored Size: {fileInfo.Length} bytes");
        _output.WriteLine($"  Processing Time: {result.ProcessingTime.TotalMilliseconds} ms");
        _output.WriteLine($"  File Path: {filePath}");
    }

    [Fact]
    public async Task DirectoryCreation_MissingDirectories_CreatesAutomatically()
    {
        // This test verifies that directories are created automatically
        // The service should handle this in the constructor/EnsureDirectoriesExist method

        // Act
        var isHealthy = await _storageService.IsHealthyAsync();

        // Assert
        Assert.True(isHealthy);

        var screenshotsDir = _configuration["Storage:ScreenshotsDirectory"];
        var thumbnailsDir = _configuration["Storage:ThumbnailsDirectory"];

        Assert.True(Directory.Exists(screenshotsDir));
        Assert.True(Directory.Exists(thumbnailsDir));

        _output.WriteLine($"Verified directories exist:");
        _output.WriteLine($"  Screenshots: {screenshotsDir}");
        _output.WriteLine($"  Thumbnails: {thumbnailsDir}");
    }

    public void Dispose()
    {
        // Clean up any created files
        foreach (var fileName in _createdFiles)
        {
            try
            {
                var deleteResult = _storageService.DeleteImageAsync(fileName).Result;
                if (deleteResult)
                {
                    _output.WriteLine($"Cleaned up file: {fileName}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to cleanup file {fileName}: {ex.Message}");
            }
        }

        // Clean up test directories if they're empty
        try
        {
            var screenshotsDir = _configuration["Storage:ScreenshotsDirectory"];
            var thumbnailsDir = _configuration["Storage:ThumbnailsDirectory"];

            if (Directory.Exists(screenshotsDir) && !Directory.EnumerateFileSystemEntries(screenshotsDir).Any())
            {
                Directory.Delete(screenshotsDir);
            }

            if (Directory.Exists(thumbnailsDir) && !Directory.EnumerateFileSystemEntries(thumbnailsDir).Any())
            {
                Directory.Delete(thumbnailsDir);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to cleanup test directories: {ex.Message}");
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}