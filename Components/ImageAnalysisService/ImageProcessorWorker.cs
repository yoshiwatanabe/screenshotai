
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Storage.Services;
using Vision.Services;

namespace ImageAnalysisService
{
    public class ImageProcessorWorker : BackgroundService
    {
        private readonly ILogger<ImageProcessorWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ProcessingChannel _channel;

        public ImageProcessorWorker(ILogger<ImageProcessorWorker> logger, IServiceProvider serviceProvider, ProcessingChannel channel)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _channel = channel;
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

                    byte[] imageBytes = await File.ReadAllBytesAsync(filePath, stoppingToken);

                    if (imageBytes.Length == 0)
                    {
                        _logger.LogWarning($"File is empty: {filePath}. Skipping analysis.");
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
                            var jsonOutputPath = Path.Combine("/home/ywatanabe/dev/screenshotai/_output", jsonFileName);
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
    }
}
