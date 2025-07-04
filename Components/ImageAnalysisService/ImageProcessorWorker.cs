
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

                    var analysisResult = await visionService.AnalyzeImageAsync(filePath, stoppingToken);

                    if (analysisResult != null)
                    {
                        var json = JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions { WriteIndented = true });
                        var jsonFilePath = Path.ChangeExtension(filePath, ".json");
                        await File.WriteAllTextAsync(jsonFilePath, json, stoppingToken);
                        _logger.LogInformation($"Analysis saved to: {jsonFilePath}");
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
