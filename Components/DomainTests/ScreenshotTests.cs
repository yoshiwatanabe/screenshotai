using ScreenshotManager.Domain.Entities;
using ScreenshotManager.Domain.Enums;
using Xunit;

namespace ScreenshotManager.Domain.Tests;

public class ScreenshotTests
{
    [Fact]
    public void CreateFromClipboard_ValidData_CreatesScreenshot()
    {
        // Arrange
        var displayName = "Test Screenshot";
        var blobName = "test-blob-123";

        // Act
        var screenshot = Screenshot.CreateFromClipboard(displayName, blobName);

        // Assert
        Assert.NotEqual(Guid.Empty, screenshot.Id);
        Assert.Equal(displayName, screenshot.DisplayName);
        Assert.Equal(blobName, screenshot.BlobName);
        Assert.Equal(ScreenshotSource.Clipboard, screenshot.Source);
        Assert.Equal(ScreenshotStatus.Processing, screenshot.Status);
        Assert.True(screenshot.CreatedAt <= DateTime.UtcNow);
        Assert.True(screenshot.CreatedAt > DateTime.UtcNow.AddSeconds(-1));
        Assert.Empty(screenshot.Tags);
        Assert.Null(screenshot.ExtractedText);
        Assert.Null(screenshot.FailureReason);
    }

    [Fact]
    public void CreateFromUpload_ValidData_CreatesScreenshot()
    {
        // Arrange
        var displayName = "Upload Screenshot";
        var blobName = "upload-blob-456";

        // Act
        var screenshot = Screenshot.CreateFromUpload(displayName, blobName);

        // Assert
        Assert.Equal(ScreenshotSource.FileUpload, screenshot.Source);
        Assert.Equal(ScreenshotStatus.Processing, screenshot.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateFromClipboard_InvalidDisplayName_ThrowsArgumentException(string? invalidDisplayName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Screenshot.CreateFromClipboard(invalidDisplayName, "valid-blob"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateFromClipboard_InvalidBlobName_ThrowsArgumentException(string? invalidBlobName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Screenshot.CreateFromClipboard("valid-name", invalidBlobName));
    }

    [Fact]
    public void UpdateDisplayName_ValidName_UpdatesSuccessfully()
    {
        // Arrange
        var screenshot = Screenshot.CreateFromClipboard("Original Name", "blob-123");
        var newName = "Updated Name";

        // Act
        screenshot.UpdateDisplayName(newName);

        // Assert
        Assert.Equal(newName, screenshot.DisplayName);
    }

    [Fact]
    public void MarkAsProcessed_ValidScreenshot_UpdatesStatus()
    {
        // Arrange
        var screenshot = Screenshot.CreateFromClipboard("Test", "blob-123");

        // Act
        screenshot.MarkAsProcessed();

        // Assert
        Assert.Equal(ScreenshotStatus.Ready, screenshot.Status);
        Assert.Null(screenshot.FailureReason);
        Assert.True(screenshot.IsReady);
        Assert.False(screenshot.IsProcessing);
    }

    [Fact]
    public void MarkAsFailed_ValidReason_UpdatesStatusAndReason()
    {
        // Arrange
        var screenshot = Screenshot.CreateFromClipboard("Test", "blob-123");
        var failureReason = "Processing failed due to invalid image format";

        // Act
        screenshot.MarkAsFailed(failureReason);

        // Assert
        Assert.Equal(ScreenshotStatus.Failed, screenshot.Status);
        Assert.Equal(failureReason, screenshot.FailureReason);
        Assert.True(screenshot.IsFailed);
        Assert.False(screenshot.IsProcessing);
    }

    [Fact]
    public void AddAIAnalysis_ValidData_UpdatesAnalysisData()
    {
        // Arrange
        var screenshot = Screenshot.CreateFromClipboard("Test", "blob-123");
        screenshot.MarkAsProcessed();
        var extractedText = "Hello World from screenshot";
        var tags = new List<string> { "text", "document", "screenshot" };

        // Act
        screenshot.AddAIAnalysis(extractedText, tags);

        // Assert
        Assert.Equal(extractedText, screenshot.ExtractedText);
        Assert.Equal(tags, screenshot.Tags);
        Assert.True(screenshot.HasAIAnalysis);
    }
}