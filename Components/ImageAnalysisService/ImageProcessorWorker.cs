
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Storage.Configuration;
using Storage.Services;
using Vision.Services;

namespace ImageAnalysisService
{
    public class ImageProcessorWorker : BackgroundService
    {
        private readonly ILogger<ImageProcessorWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ProcessingChannel _channel;
        private readonly StorageOptions _storageOptions;

        public ImageProcessorWorker(ILogger<ImageProcessorWorker> logger, IServiceProvider serviceProvider, ProcessingChannel channel, IOptions<StorageOptions> storageOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _channel = channel;
            _storageOptions = storageOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Image Processor Service starting.");

            await foreach (var filePath in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var visionService = scope.ServiceProvider.GetRequiredService<AzureVisionHttpService>();
                    var storageService = scope.ServiceProvider.GetRequiredService<LocalFileStorageService>();

                    _logger.LogInformation($"Processing file: {filePath}");

                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning($"File not found: {filePath}. Skipping processing.");
                        continue;
                    }

                    byte[] imageBytes = await ReadFileWithRetryAsync(filePath, stoppingToken);

                    if (imageBytes.Length == 0)
                    {
                        _logger.LogWarning($"File is empty after retries: {filePath}. Skipping analysis.");
                        continue;
                    }

                    var analysisResult = await visionService.AnalyzeImageAsync(imageBytes, stoppingToken);

                    if (analysisResult != null)
                    {
                        // Save image to output directory using Storage service
                        var fileName = Path.GetFileName(filePath);
                        var uploadResult = await storageService.UploadFromClipboardAsync(imageBytes, fileName, stoppingToken);

                        if (uploadResult != null && uploadResult.Success)
                        {
                            // Save analysis JSON to output directory with matching filename
                            var json = JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions { WriteIndented = true });
                            var jsonFileName = Path.ChangeExtension(uploadResult.BlobName, ".json");
                            var jsonOutputPath = Path.Combine(_storageOptions.ScreenshotsDirectory, jsonFileName);
                            await File.WriteAllTextAsync(jsonOutputPath, json, stoppingToken);

                            _logger.LogInformation($"Image saved as: {uploadResult.BlobName}");
                            _logger.LogInformation($"Analysis saved to: {jsonOutputPath}");

                            // Clean up the original file from _watch
                            File.Delete(filePath);
                            _logger.LogInformation($"Cleaned up original file: {filePath}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to save image using storage service: {fileName}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Azure Vision analysis returned no result for file: {filePath}. Check previous logs for errors or if simulation is enabled.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing file: {filePath}");
                }
            }
        }

        private async Task<byte[]> ReadFileWithRetryAsync(string filePath, CancellationToken cancellationToken)
        {
            const int maxRetries = 5;
            const int delayMs = 500;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        return Array.Empty<byte>();

                    if (!IsFileReady(filePath))
                    {
                        _logger.LogDebug($"File not ready, attempt {attempt + 1}/{maxRetries}: {filePath}");
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    
                    if (bytes.Length > 0)
                    {
                        _logger.LogDebug($"Successfully read {bytes.Length} bytes from {filePath}");
                        return bytes;
                    }
                    
                    _logger.LogDebug($"File is empty, attempt {attempt + 1}/{maxRetries}: {filePath}");
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    _logger.LogDebug($"File locked by another process, attempt {attempt + 1}/{maxRetries}: {filePath}");
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogDebug($"Access denied, attempt {attempt + 1}/{maxRetries}: {filePath}. {ex.Message}");
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error reading file {filePath}, attempt {attempt + 1}/{maxRetries}");
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            _logger.LogWarning($"Failed to read file after {maxRetries} attempts: {filePath}");
            return Array.Empty<byte>();
        }

        private bool IsFileReady(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return stream.Length > 0;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
