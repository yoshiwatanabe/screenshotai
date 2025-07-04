using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ImageAnalysisService;
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
        var testFilePath = "/tmp/test_image.png";
        await File.WriteAllBytesAsync(testFilePath, new byte[] { 0x01, 0x02, 0x03 });

        // Act
        await channel.Writer.WriteAsync(testFilePath);
        channel.Writer.Complete();

        // Assert
        var result = await channel.Reader.ReadAsync();
        Assert.Equal(testFilePath, result);

        // Clean up
        if (File.Exists(testFilePath))
            File.Delete(testFilePath);
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

        // Act & Assert
        var worker = new ImageProcessorWorker(logger, serviceProvider, channel);
        Assert.NotNull(worker);
    }
}