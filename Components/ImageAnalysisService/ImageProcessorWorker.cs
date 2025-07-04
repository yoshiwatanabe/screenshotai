
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Storage.Services;

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
                        var json = JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions { WriteIndented = true });
                        var jsonFilePath = Path.ChangeExtension(filePath, ".json");
                        await File.WriteAllTextAsync(jsonFilePath, json, stoppingToken);
                        _logger.LogInformation($"Analysis saved to: {jsonFilePath}");
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
