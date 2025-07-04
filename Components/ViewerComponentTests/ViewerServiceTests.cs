using Xunit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using ViewerComponent.Services;
using ViewerComponent.Configuration;
using System.IO;

namespace ViewerComponentTests;

public class ViewerServiceTests : IDisposable
{
    private readonly ViewerService _viewerService;
    private readonly Mock<ILogger<ViewerService>> _mockLogger;
    private readonly string _testDirectory;
    private readonly ViewerOptions _options;

    public ViewerServiceTests()
    {
        _mockLogger = new Mock<ILogger<ViewerService>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), "ViewerComponentTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _options = new ViewerOptions
        {
            OutputDirectory = _testDirectory
        };

        var mockOptions = new Mock<IOptions<ViewerOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);

        _viewerService = new ViewerService(mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetImageFilesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Act
        var result = await _viewerService.GetImageFilesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetImageFilesAsync_WithImageFiles_ReturnsImageFileInfos()
    {
        // Arrange
        var imagePath1 = Path.Combine(_testDirectory, "test1.png");
        var imagePath2 = Path.Combine(_testDirectory, "test2.png");
        var jsonPath1 = Path.Combine(_testDirectory, "test1.json");

        await File.WriteAllBytesAsync(imagePath1, new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        await File.WriteAllBytesAsync(imagePath2, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllTextAsync(jsonPath1, "{}");

        // Act
        var result = await _viewerService.GetImageFilesAsync();

        // Assert
        Assert.Equal(2, result.Count());
        
        var imageWithAnalysis = result.First(x => x.FileName == "test1.png");
        var imageWithoutAnalysis = result.First(x => x.FileName == "test2.png");
        
        Assert.True(imageWithAnalysis.HasAnalysis);
        Assert.False(imageWithoutAnalysis.HasAnalysis);
    }

    [Fact]
    public async Task GetImageAsync_ExistingFile_ReturnsImageData()
    {
        // Arrange
        var imagePath = Path.Combine(_testDirectory, "test.png");
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await File.WriteAllBytesAsync(imagePath, imageData);

        // Act
        var result = await _viewerService.GetImageAsync("test.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(imageData, result);
    }

    [Fact]
    public async Task GetImageAsync_NonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _viewerService.GetImageAsync("nonexistent.png");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAnalysisAsync_ExistingFile_ReturnsJsonData()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "test.json");
        var jsonData = "{\"test\": \"data\"}";
        await File.WriteAllTextAsync(jsonPath, jsonData);

        // Act
        var result = await _viewerService.GetAnalysisAsync("test.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jsonData, result);
    }

    [Fact]
    public async Task GetAnalysisAsync_NonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _viewerService.GetAnalysisAsync("nonexistent.png");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatusAsync_WithFiles_ReturnsCorrectStatus()
    {
        // Arrange
        var imagePath1 = Path.Combine(_testDirectory, "test1.png");
        var imagePath2 = Path.Combine(_testDirectory, "test2.png");
        var jsonPath1 = Path.Combine(_testDirectory, "test1.json");

        await File.WriteAllBytesAsync(imagePath1, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllBytesAsync(imagePath2, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllTextAsync(jsonPath1, "{}");

        // Act
        var result = await _viewerService.GetStatusAsync();

        // Assert
        Assert.Equal(2, result.TotalImages);
        Assert.Equal(1, result.ImagesWithAnalysis);
        Assert.Equal(_testDirectory, result.OutputDirectory);
    }

    [Fact]
    public async Task GetImageAsync_PathTraversal_ReturnsNull()
    {
        // Arrange - Try to access file outside the output directory
        var filename = "../../../etc/passwd";

        // Act
        var result = await _viewerService.GetImageAsync(filename);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAnalysisAsync_PathTraversal_ReturnsNull()
    {
        // Arrange - Try to access file outside the output directory
        var filename = "../../../etc/passwd";

        // Act
        var result = await _viewerService.GetAnalysisAsync(filename);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetImageFilesAsync_NonExistentDirectory_ReturnsEmptyList()
    {
        // Arrange
        _options.OutputDirectory = "/non/existent/directory";

        // Act
        var result = await _viewerService.GetImageFilesAsync();

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}