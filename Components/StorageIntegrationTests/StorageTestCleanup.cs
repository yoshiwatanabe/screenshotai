using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StorageIntegrationTests;

public class StorageTestCleanup
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<StorageTestCleanup> _logger;
    private readonly string _screenshotsContainer;
    private readonly string _thumbnailsContainer;

    public StorageTestCleanup(IConfiguration configuration, ILogger<StorageTestCleanup> logger)
    {
        _logger = logger;
        var connectionString = configuration["Storage:ConnectionString"];
        _screenshotsContainer = configuration["Storage:ContainerName"] ?? "integration-test-screenshots";
        _thumbnailsContainer = configuration["Storage:ThumbnailContainer"] ?? "integration-test-thumbnails";

        if (string.IsNullOrEmpty(connectionString) || connectionString == "PLACEHOLDER_FOR_USER_SECRETS")
        {
            throw new InvalidOperationException("Azure Storage connection string not configured for cleanup");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task CleanupTestContainersAsync()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of test containers...");

            var cleanupTasks = new[]
            {
                CleanupContainerAsync(_screenshotsContainer),
                CleanupContainerAsync(_thumbnailsContainer)
            };

            await Task.WhenAll(cleanupTasks);
            
            _logger.LogInformation("Test container cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup test containers");
            throw;
        }
    }

    public async Task CleanupTestBlobsAsync(IEnumerable<string> blobNames)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of specific test blobs...");

            var screenshotsContainer = _blobServiceClient.GetBlobContainerClient(_screenshotsContainer);
            var thumbnailsContainer = _blobServiceClient.GetBlobContainerClient(_thumbnailsContainer);

            var cleanupTasks = blobNames.SelectMany(blobName => new[]
            {
                CleanupBlobAsync(screenshotsContainer, blobName),
                CleanupBlobAsync(thumbnailsContainer, GenerateThumbnailBlobName(blobName))
            });

            await Task.WhenAll(cleanupTasks);
            
            _logger.LogInformation("Test blob cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup test blobs");
            throw;
        }
    }

    public async Task<bool> ContainerExistsAsync(string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var response = await containerClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if container {ContainerName} exists", containerName);
            return false;
        }
    }

    public async Task<int> CountBlobsInContainerAsync(string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            if (!await containerClient.ExistsAsync())
            {
                return 0;
            }

            var blobCount = 0;
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobCount++;
            }

            return blobCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count blobs in container {ContainerName}", containerName);
            return -1;
        }
    }

    public async Task DeleteContainerIfExistsAsync(string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.DeleteIfExistsAsync();
            _logger.LogInformation("Deleted container: {ContainerName}", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete container {ContainerName}", containerName);
        }
    }

    private async Task CleanupContainerAsync(string containerName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            if (!await containerClient.ExistsAsync())
            {
                _logger.LogInformation("Container {ContainerName} does not exist, skipping cleanup", containerName);
                return;
            }

            var deleteTasks = new List<Task>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                deleteTasks.Add(CleanupBlobAsync(containerClient, blobItem.Name));
                
                // Process in batches to avoid overwhelming the service
                if (deleteTasks.Count >= 10)
                {
                    await Task.WhenAll(deleteTasks);
                    deleteTasks.Clear();
                }
            }

            // Process remaining blobs
            if (deleteTasks.Count > 0)
            {
                await Task.WhenAll(deleteTasks);
            }

            _logger.LogInformation("Cleaned up all blobs in container: {ContainerName}", containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup container {ContainerName}", containerName);
        }
    }

    private async Task CleanupBlobAsync(BlobContainerClient containerClient, string blobName)
    {
        try
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
            _logger.LogDebug("Deleted blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob {BlobName}", blobName);
        }
    }

    private static string GenerateThumbnailBlobName(string blobName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);
        return $"{nameWithoutExtension}_thumb.jpg";
    }
}

public static class StorageTestCleanupExtensions
{
    public static async Task<StorageTestCleanup> CreateCleanupServiceAsync(IConfiguration configuration, ILogger<StorageTestCleanup> logger)
    {
        var cleanup = new StorageTestCleanup(configuration, logger);
        return cleanup;
    }
}