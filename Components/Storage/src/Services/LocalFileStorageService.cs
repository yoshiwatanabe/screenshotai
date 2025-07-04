using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Storage.Configuration;
using Storage.Exceptions;
using Storage.Models;
using System.Diagnostics;

namespace Storage.Services;

public class LocalFileStorageService : ILocalStorageService, IScreenshotStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;
    public LocalFileStorageService(
        IOptions<StorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        EnsureDirectoriesExist();
    }

    public async Task<UploadResult> UploadFromClipboardAsync(
        byte[] imageData,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting local upload of clipboard image: {FileName}", fileName);

            var optimizedResult = await OptimizeImageAsync(imageData, _options.DefaultOptimization);
            
            var uniqueFileName = GenerateUniqueFileName(fileName);
            var imagePath = Path.Combine(_options.ScreenshotsDirectory, uniqueFileName);
            var thumbnailFileName = GenerateThumbnailFileName(uniqueFileName);
            var thumbnailPath = Path.Combine(_options.ThumbnailsDirectory, thumbnailFileName);

            var uploadTasks = new[]
            {
                File.WriteAllBytesAsync(imagePath, optimizedResult.OptimizedImageData, cancellationToken),
                File.WriteAllBytesAsync(thumbnailPath, optimizedResult.ThumbnailData, cancellationToken)
            };

            await Task.WhenAll(uploadTasks);

            stopwatch.Stop();
            
            _logger.LogInformation("Successfully saved clipboard image locally: {FileName} in {ProcessingTime}ms", 
                fileName, stopwatch.ElapsedMilliseconds);

            return new UploadResult
            {
                Success = true,
                BlobName = uniqueFileName,
                ThumbnailBlobName = thumbnailFileName,
                FileSizeBytes = optimizedResult.OptimizedSize,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to save clipboard image locally: {FileName}", fileName);
            
            return new UploadResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<OptimizedImageResult> OptimizeImageAsync(
        byte[] originalImage,
        ImageOptimizationSettings settings)
    {
        try
        {
            using var image = Image.Load(originalImage);
            
            var originalSize = originalImage.Length;
            
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(settings.MaxWidth, settings.MaxHeight),
                Mode = ResizeMode.Max
            }));

            using var optimizedStream = new MemoryStream();
            await image.SaveAsJpegAsync(optimizedStream, new JpegEncoder
            {
                Quality = settings.JpegQuality
            });

            var optimizedData = optimizedStream.ToArray();
            var thumbnailData = await GenerateThumbnailAsync(image, settings.ThumbnailSize);

            return new OptimizedImageResult
            {
                OptimizedImageData = optimizedData,
                ThumbnailData = thumbnailData,
                OriginalSize = originalSize,
                OptimizedSize = optimizedData.Length,
                CompressionRatio = (double)optimizedData.Length / originalSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize image");
            throw new StorageException(StorageErrorCode.InvalidImageFormat, string.Empty, "Failed to optimize image", ex);
        }
    }

    public async Task<Stream> GetImageStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var imagePath = Path.Combine(_options.ScreenshotsDirectory, fileName);
            
            if (!File.Exists(imagePath))
            {
                throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Image file not found: {fileName}");
            }

            var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return fileStream;
        }
        catch (StorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image stream for file: {FileName}", fileName);
            throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Failed to get image stream for file: {fileName}", ex);
        }
    }

    public Task<string> GetThumbnailPathAsync(string fileName)
    {
        try
        {
            var thumbnailFileName = GenerateThumbnailFileName(fileName);
            var thumbnailPath = Path.Combine(_options.ThumbnailsDirectory, thumbnailFileName);
            
            if (!File.Exists(thumbnailPath))
            {
                throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Thumbnail file not found: {thumbnailFileName}");
            }

            return Task.FromResult(thumbnailPath);
        }
        catch (StorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail path for file: {FileName}", fileName);
            throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Failed to get thumbnail path for file: {fileName}", ex);
        }
    }

    public Task<string> GetImagePathAsync(string fileName)
    {
        try
        {
            var imagePath = Path.Combine(_options.ScreenshotsDirectory, fileName);
            
            if (!File.Exists(imagePath))
            {
                throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Image file not found: {fileName}");
            }

            return Task.FromResult(imagePath);
        }
        catch (StorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image path for file: {FileName}", fileName);
            throw new StorageException(StorageErrorCode.BlobNotFound, fileName, $"Failed to get image path for file: {fileName}", ex);
        }
    }

    public Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var imagePath = Path.Combine(_options.ScreenshotsDirectory, fileName);
            var thumbnailFileName = GenerateThumbnailFileName(fileName);
            var thumbnailPath = Path.Combine(_options.ThumbnailsDirectory, thumbnailFileName);

            var deletedAny = false;

            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
                deletedAny = true;
                _logger.LogDebug("Deleted image file: {ImagePath}", imagePath);
            }

            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
                deletedAny = true;
                _logger.LogDebug("Deleted thumbnail file: {ThumbnailPath}", thumbnailPath);
            }

            return Task.FromResult(deletedAny);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image: {FileName}", fileName);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if directories exist and are writable
            EnsureDirectoriesExist();
            
            // Test write access by creating a temporary file
            var testFilePath = Path.Combine(_options.ScreenshotsDirectory, ".healthcheck");
            await File.WriteAllTextAsync(testFilePath, "healthcheck", cancellationToken);
            File.Delete(testFilePath);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return false;
        }
    }

    private void EnsureDirectoriesExist()
    {
        try
        {
            Directory.CreateDirectory(_options.ScreenshotsDirectory);
            Directory.CreateDirectory(_options.ThumbnailsDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create storage directories");
            throw new StorageException(StorageErrorCode.InsufficientPermissions, string.Empty, "Failed to create storage directories", ex);
        }
    }

    private async Task<byte[]> GenerateThumbnailAsync(Image image, System.Drawing.Size thumbnailSize)
    {
        using var thumbnail = image.Clone(ctx => {});
        thumbnail.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(thumbnailSize.Width, thumbnailSize.Height),
            Mode = ResizeMode.Max
        }));

        using var thumbnailStream = new MemoryStream();
        await thumbnail.SaveAsJpegAsync(thumbnailStream, new JpegEncoder { Quality = 80 });
        return thumbnailStream.ToArray();
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        
        // Clean the filename to be filesystem safe
        var cleanName = string.Join("_", nameWithoutExtension.Split(Path.GetInvalidFileNameChars()));
        
        return $"{timestamp}_{guid}_{cleanName}{extension}";
    }

    private static string GenerateThumbnailFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return $"{nameWithoutExtension}_thumb.jpg";
    }
}