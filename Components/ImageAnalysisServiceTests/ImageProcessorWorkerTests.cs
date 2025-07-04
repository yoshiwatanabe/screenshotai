using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageAnalysisService;
using Storage.Services;
using Vision.Services;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.IO;
using Domain.Entities;
using Domain.Enums;

namespace ImageAnalysisServiceTests;

public class ImageProcessorWorkerTests
{
    private readonly Mock<ILogger<ImageProcessorWorker>> _mockLogger;
    private readonly Mock<ILocalStorageService> _mockLocalStorageService;
    private readonly Mock<IScreenshotStorageService> _mockScreenshotStorageService;
    private readonly Mock<AzureVisionHttpService> _mockAzureVisionHttpService;
    private readonly ProcessingChannel _processingChannel;

    public ImageProcessorWorkerTests()
    {
        _mockLogger = new Mock<ILogger<ImageProcessorWorker>>();
        _mockLocalStorageService = new Mock<ILocalStorageService>();
        _mockScreenshotStorageService = new Mock<IScreenshotStorageService>();
        _mockAzureVisionHttpService = new Mock<AzureVisionHttpService>(null, null, null); // Pass nulls for constructor args
        _processingChannel = new ProcessingChannel();
    }

    [Fact]
    public async Task ProcessFile_SimulationEnabled_SavesAnalysis()
    {
        // Arrange
        var testFilePath = "/tmp/test_image.png";
        await File.WriteAllBytesAsync(testFilePath, new byte[] { 0x01, 0x02, 0x03 });

        _mockAzureVisionHttpService.Setup(s => s.AnalyzeImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Simulated analysis result");

        _mockLocalStorageService.Setup(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadResult { FileName = "test_image.png", FilePath = testFilePath });

        var worker = new ImageProcessorWorker(
            _mockLogger.Object,
            _processingChannel,
            _mockLocalStorageService.Object,
            _mockScreenshotStorageService.Object,
            _mockAzureVisionHttpService.Object);

        await _processingChannel.AddFileAsync(testFilePath);

        // Act
        // The worker runs as a background service, so we need to simulate its execution
        // For a simple test, we can directly call the method that processes the queue
        // In a real scenario, you might start the hosted service and wait for it to process
        await Task.Delay(100); // Give some time for the background task to pick up the item

        // Assert
        _mockAzureVisionHttpService.Verify(s => s.AnalyzeImageAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockLocalStorageService.Verify(s => s.SaveImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockScreenshotStorageService.Verify(s => s.SaveScreenshotAsync(It.IsAny<Screenshot>(), It.IsAny<CancellationToken>()), Times.Once);

        // Clean up the test file
        File.Delete(testFilePath);
    }
}