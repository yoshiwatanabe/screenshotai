using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Services;
using Xunit;
using Xunit.Abstractions;

namespace StorageIntegrationTests;

public class CleanupTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<CleanupTests> _logger;

    public CleanupTests(ITestOutputHelper output)
    {
        _output = output;
        _serviceProvider = TestConfiguration.CreateServiceProvider();
        _configuration = TestConfiguration.BuildConfiguration();
        _logger = _serviceProvider.GetRequiredService<ILogger<CleanupTests>>();
    }

    [Fact]
    public async Task CleanupService_CanCountBlobsInContainer()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var cleanup = new StorageTestCleanup(_configuration, _logger);
        var storageService = _serviceProvider.GetRequiredService<IBlobStorageService>();

        // First, upload a test image to ensure there's something to count
        var testImageData = TestImageHelper.CreateTestJpegImage(100, 100);
        var uploadResult = await storageService.UploadFromClipboardAsync(testImageData, "cleanup_test.jpg");
        Assert.True(uploadResult.Success);

        // Act
        var screenshotCount = await cleanup.CountBlobsInContainerAsync("integration-test-screenshots");
        var thumbnailCount = await cleanup.CountBlobsInContainerAsync("integration-test-thumbnails");

        // Assert
        Assert.True(screenshotCount >= 1, "Should have at least one screenshot blob");
        Assert.True(thumbnailCount >= 1, "Should have at least one thumbnail blob");

        _output.WriteLine($"Found {screenshotCount} screenshot blobs and {thumbnailCount} thumbnail blobs");

        // Cleanup the test blob
        await storageService.DeleteImageAsync(uploadResult.BlobName);
    }

    [Fact]
    public async Task CleanupService_CanCheckContainerExists()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var cleanup = new StorageTestCleanup(_configuration, _logger);

        // Act
        var screenshotsExists = await cleanup.ContainerExistsAsync("integration-test-screenshots");
        var thumbnailsExists = await cleanup.ContainerExistsAsync("integration-test-thumbnails");
        var nonExistentExists = await cleanup.ContainerExistsAsync("non-existent-container");

        // Assert
        _output.WriteLine($"Screenshots container exists: {screenshotsExists}");
        _output.WriteLine($"Thumbnails container exists: {thumbnailsExists}");
        _output.WriteLine($"Non-existent container exists: {nonExistentExists}");

        Assert.False(nonExistentExists, "Non-existent container should not exist");
    }

    [Fact]
    public async Task CleanupSpecificBlobs_WorksCorrectly()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var cleanup = new StorageTestCleanup(_configuration, _logger);
        var storageService = _serviceProvider.GetRequiredService<IBlobStorageService>();

        // Upload a few test images
        var testImageData = TestImageHelper.CreateTestJpegImage(200, 200);
        var uploadResults = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            var uploadResult = await storageService.UploadFromClipboardAsync(testImageData, $"cleanup_specific_test_{i}.jpg");
            Assert.True(uploadResult.Success);
            uploadResults.Add(uploadResult.BlobName);
        }

        _output.WriteLine($"Uploaded {uploadResults.Count} test images for cleanup");

        // Act - Cleanup specific blobs
        await cleanup.CleanupTestBlobsAsync(uploadResults);

        // Assert - Try to retrieve the blobs (should fail)
        foreach (var blobName in uploadResults)
        {
            await Assert.ThrowsAsync<Storage.Exceptions.StorageException>(
                () => storageService.GetImageStreamAsync(blobName));
        }

        _output.WriteLine("Successfully cleaned up all specific test blobs");
    }

    [Fact(Skip = "Manual test - only run when you want to completely clean test containers")]
    public async Task ManualCleanup_AllTestContainers()
    {
        // Skip if Azure Storage is not configured
        if (!TestConfiguration.IsAzureStorageConfigured(_configuration))
        {
            _output.WriteLine("Skipping test: Azure Storage connection string not configured");
            return;
        }

        // Arrange
        var cleanup = new StorageTestCleanup(_configuration, _logger);

        // Count blobs before cleanup
        var screenshotsBefore = await cleanup.CountBlobsInContainerAsync("integration-test-screenshots");
        var thumbnailsBefore = await cleanup.CountBlobsInContainerAsync("integration-test-thumbnails");

        _output.WriteLine($"Before cleanup: {screenshotsBefore} screenshots, {thumbnailsBefore} thumbnails");

        // Act
        await cleanup.CleanupTestContainersAsync();

        // Assert
        var screenshotsAfter = await cleanup.CountBlobsInContainerAsync("integration-test-screenshots");
        var thumbnailsAfter = await cleanup.CountBlobsInContainerAsync("integration-test-thumbnails");

        _output.WriteLine($"After cleanup: {screenshotsAfter} screenshots, {thumbnailsAfter} thumbnails");

        Assert.True(screenshotsAfter < screenshotsBefore || screenshotsBefore == 0);
        Assert.True(thumbnailsAfter < thumbnailsBefore || thumbnailsBefore == 0);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}