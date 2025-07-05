using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ImageAnalysisService;
using Storage.Configuration;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Threading;
using System.IO;

namespace ImageAnalysisServiceTests;

public class ImageProcessorWorkerTests
{
    [Fact]
    public async Task ProcessingChannel_CanAddAndReadFiles()
    {
        // Arrange
        var channel = new ProcessingChannel();
        var tempDir = Path.Combine(Path.GetTempPath(), "ImageAnalysisServiceTests");
        Directory.CreateDirectory(tempDir);
        var testFilePath = Path.Combine(tempDir, "test_image.png");
        await File.WriteAllBytesAsync(testFilePath, new byte[] { 0x01, 0x02, 0x03 });

        try
        {
            // Act
            await channel.Writer.WriteAsync(testFilePath);
            channel.Writer.Complete();

            // Assert
            var result = await channel.Reader.ReadAsync();
            Assert.Equal(testFilePath, result);
        }
        finally
        {
            // Clean up
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    [Fact]
    public async Task ImageProcessorWorker_CanBeInstantiated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetService<ILogger<ImageProcessorWorker>>();
        var channel = new ProcessingChannel();
        var storageOptions = Options.Create(new StorageOptions { ScreenshotsDirectory = "_output" });

        // Act & Assert
        var worker = new ImageProcessorWorker(logger, serviceProvider, channel, storageOptions);
        Assert.NotNull(worker);
    }
}