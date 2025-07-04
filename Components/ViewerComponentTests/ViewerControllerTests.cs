using Xunit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ViewerComponent.Controllers;
using ViewerComponent.Services;
using ViewerComponent.Models;

namespace ViewerComponentTests;

public class ViewerControllerTests
{
    private readonly ViewerController _controller;
    private readonly Mock<IViewerService> _mockViewerService;

    public ViewerControllerTests()
    {
        _mockViewerService = new Mock<IViewerService>();
        _controller = new ViewerController(_mockViewerService.Object);
    }

    [Fact]
    public async Task GetFiles_ReturnsOkWithFiles()
    {
        // Arrange
        var expectedFiles = new List<ImageFileInfo>
        {
            new ImageFileInfo
            {
                FileName = "test1.png",
                ImagePath = "test1.png",
                JsonPath = "test1.json",
                CreatedAt = DateTime.UtcNow,
                FileSize = 1024
            },
            new ImageFileInfo
            {
                FileName = "test2.png",
                ImagePath = "test2.png",
                JsonPath = null,
                CreatedAt = DateTime.UtcNow,
                FileSize = 2048
            }
        };

        _mockViewerService.Setup(s => s.GetImageFilesAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedFiles);

        // Act
        var result = await _controller.GetFiles(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualFiles = Assert.IsAssignableFrom<IEnumerable<ImageFileInfo>>(okResult.Value);
        Assert.Equal(2, actualFiles.Count());
        
        _mockViewerService.Verify(s => s.GetImageFilesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetImage_ExistingFile_ReturnsFileResult()
    {
        // Arrange
        var filename = "test.png";
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        _mockViewerService.Setup(s => s.GetImageAsync(filename, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(imageData);

        // Act
        var result = await _controller.GetImage(filename, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(imageData, fileResult.FileContents);
        
        _mockViewerService.Verify(s => s.GetImageAsync(filename, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetImage_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var filename = "nonexistent.png";
        
        _mockViewerService.Setup(s => s.GetImageAsync(filename, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _controller.GetImage(filename, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        _mockViewerService.Verify(s => s.GetImageAsync(filename, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAnalysis_ExistingFile_ReturnsContentResult()
    {
        // Arrange
        var filename = "test.png";
        var analysisData = "{\"test\": \"data\"}";
        
        _mockViewerService.Setup(s => s.GetAnalysisAsync(filename, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(analysisData);

        // Act
        var result = await _controller.GetAnalysis(filename, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("application/json", contentResult.ContentType);
        Assert.Equal(analysisData, contentResult.Content);
        
        _mockViewerService.Verify(s => s.GetAnalysisAsync(filename, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAnalysis_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var filename = "nonexistent.png";
        
        _mockViewerService.Setup(s => s.GetAnalysisAsync(filename, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.GetAnalysis(filename, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        _mockViewerService.Verify(s => s.GetAnalysisAsync(filename, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatus()
    {
        // Arrange
        var expectedStatus = new ViewerStatus
        {
            TotalImages = 5,
            ImagesWithAnalysis = 3,
            LastUpdated = DateTime.UtcNow,
            OutputDirectory = "/test/output"
        };

        _mockViewerService.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetStatus(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualStatus = Assert.IsType<ViewerStatus>(okResult.Value);
        Assert.Equal(expectedStatus.TotalImages, actualStatus.TotalImages);
        Assert.Equal(expectedStatus.ImagesWithAnalysis, actualStatus.ImagesWithAnalysis);
        Assert.Equal(expectedStatus.OutputDirectory, actualStatus.OutputDirectory);
        
        _mockViewerService.Verify(s => s.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFiles_CancellationRequested_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockViewerService.Setup(s => s.GetImageFilesAsync(It.IsAny<CancellationToken>()))
                         .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _controller.GetFiles(cts.Token));
        
        _mockViewerService.Verify(s => s.GetImageFilesAsync(cts.Token), Times.Once);
    }
}