using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Models;
using Storage.Services;
using Xunit;
using Xunit.Abstractions;

namespace StorageIntegrationTests;

public class AzureStorageIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBlobStorageService _storageService;
    private readonly ILogger<AzureStorageIntegrationTests> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<string> _createdBlobs = [];
    private readonly ITestOutputHelper _output;

    public AzureStorageIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _serviceProvider = TestConfiguration.CreateServiceProvider();
        _storageService = _serviceProvider.GetRequiredService<IBlobStorageService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<AzureStorageIntegrationTests>>();
        _configuration = TestConfiguration.BuildConfiguration();
    }

    [Fact]
    public async Task UploadFromClipboardAsync_ValidImage_UploadsSuccessfully()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var testImageData = TestImageHelper.CreateTestJpegImage(800, 600);
        var fileName = "integration_test_clipboard.jpg";

        // Act
        var result = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        // Assert
        Assert.True(result.Success, $"Upload failed: {result.ErrorMessage}");
        Assert.NotEmpty(result.BlobName);
        Assert.NotEmpty(result.ThumbnailBlobName);
        Assert.True(result.FileSizeBytes > 0);
        Assert.True(result.ProcessingTime > TimeSpan.Zero);

        // Track for cleanup
        _createdBlobs.Add(result.BlobName);

        _output.WriteLine($"Upload successful:");
        _output.WriteLine($"  Blob Name: {result.BlobName}");
        _output.WriteLine($"  Thumbnail: {result.ThumbnailBlobName}");
        _output.WriteLine($"  File Size: {result.FileSizeBytes} bytes");
        _output.WriteLine($"  Processing Time: {result.ProcessingTime.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task GetImageStreamAsync_ExistingBlob_ReturnsStream()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(400, 300);
        var fileName = "integration_test_retrieve.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);
        
        Assert.True(uploadResult.Success);
        _createdBlobs.Add(uploadResult.BlobName);

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
    public async Task GetThumbnailUriAsync_ExistingBlob_ReturnsValidUri()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(1200, 800);
        var fileName = "integration_test_thumbnail.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);
        
        Assert.True(uploadResult.Success);
        _createdBlobs.Add(uploadResult.BlobName);

        // Act
        var thumbnailUri = await _storageService.GetThumbnailUriAsync(uploadResult.BlobName);

        // Assert
        Assert.NotNull(thumbnailUri);
        Assert.True(thumbnailUri.IsAbsoluteUri);
        Assert.Contains("integration-test-thumbnails", thumbnailUri.ToString());
        
        _output.WriteLine($"Thumbnail URI: {thumbnailUri}");
    }

    [Fact]
    public async Task OptimizeImageAsync_LargeImage_ReducesSize()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

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
    public async Task DeleteImageAsync_ExistingBlob_DeletesSuccessfully()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange - First upload an image
        var testImageData = TestImageHelper.CreateTestJpegImage(600, 400);
        var fileName = "integration_test_delete.jpg";
        var uploadResult = await _storageService.UploadFromClipboardAsync(testImageData, fileName);
        
        Assert.True(uploadResult.Success);

        // Act
        var deleteResult = await _storageService.DeleteImageAsync(uploadResult.BlobName);

        // Assert
        Assert.True(deleteResult);

        // Verify the blob is actually deleted by trying to retrieve it
        await Assert.ThrowsAsync<Storage.Exceptions.StorageException>(
            () => _storageService.GetImageStreamAsync(uploadResult.BlobName));

        _output.WriteLine($"Successfully deleted blob: {uploadResult.BlobName}");
    }

    [Fact]
    public async Task IsHealthyAsync_ValidConfiguration_ReturnsTrue()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Act
        var isHealthy = await _storageService.IsHealthyAsync();

        // Assert
        Assert.True(isHealthy);
        _output.WriteLine("Storage service health check passed");
    }

    [Fact]
    public async Task FullWorkflow_UploadOptimizeRetrieveDelete_WorksEndToEnd()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var originalImageData = TestImageHelper.CreateTestJpegImage(1600, 1200);
        var fileName = "integration_test_workflow.jpg";

        _output.WriteLine("Starting full workflow test...");

        // Act & Assert - Upload
        var uploadResult = await _storageService.UploadFromClipboardAsync(originalImageData, fileName);
        Assert.True(uploadResult.Success);
        _output.WriteLine($"✓ Upload completed: {uploadResult.BlobName}");

        // Act & Assert - Retrieve
        using var retrievedStream = await _storageService.GetImageStreamAsync(uploadResult.BlobName);
        Assert.NotNull(retrievedStream);
        _output.WriteLine("✓ Image retrieved successfully");

        // Act & Assert - Thumbnail
        var thumbnailUri = await _storageService.GetThumbnailUriAsync(uploadResult.BlobName);
        Assert.NotNull(thumbnailUri);
        _output.WriteLine($"✓ Thumbnail URI generated: {thumbnailUri}");

        // Act & Assert - Health Check
        var isHealthy = await _storageService.IsHealthyAsync();
        Assert.True(isHealthy);
        _output.WriteLine("✓ Health check passed");

        // Act & Assert - Delete
        var deleteResult = await _storageService.DeleteImageAsync(uploadResult.BlobName);
        Assert.True(deleteResult);
        _output.WriteLine("✓ Image deleted successfully");

        _output.WriteLine("Full workflow test completed successfully!");
    }

    [Theory]
    [InlineData(100, 100, 50)] // Small image, high quality
    [InlineData(1920, 1080, 85)] // Standard resolution
    [InlineData(3840, 2160, 95)] // 4K resolution
    public async Task UploadFromClipboardAsync_DifferentImageSizes_HandlesCorrectly(int width, int height, int quality)
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var testImageData = TestImageHelper.CreateTestJpegImage(width, height, quality);
        var fileName = $"integration_test_{width}x{height}_q{quality}.jpg";

        // Act
        var result = await _storageService.UploadFromClipboardAsync(testImageData, fileName);

        // Assert
        Assert.True(result.Success, $"Upload failed for {width}x{height}: {result.ErrorMessage}");
        
        // Track for cleanup
        _createdBlobs.Add(result.BlobName);

        _output.WriteLine($"Successfully uploaded {width}x{height} image:");
        _output.WriteLine($"  Original Size: {testImageData.Length} bytes");
        _output.WriteLine($"  Stored Size: {result.FileSizeBytes} bytes");
        _output.WriteLine($"  Processing Time: {result.ProcessingTime.TotalMilliseconds} ms");
    }

    public void Dispose()
    {
        // Clean up any created blobs
        if (TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            var cleanupTasks = _createdBlobs.Select(async blobName =>
            {
                try
                {
                    await _storageService.DeleteImageAsync(blobName);
                    _output.WriteLine($"Cleaned up blob: {blobName}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Failed to cleanup blob {blobName}: {ex.Message}");
                }
            });

            Task.WaitAll(cleanupTasks.ToArray(), TimeSpan.FromSeconds(30));
        }

        _serviceProvider.Dispose();
    }
}