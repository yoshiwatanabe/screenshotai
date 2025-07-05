using ScreenshotManager.Domain.ValueObjects;
using Xunit;

namespace ScreenshotManager.Domain.Tests;

public class BlobReferenceTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesBlobReference()
    {
        // Arrange
        var containerName = "screenshots";
        var blobName = "test-blob.png";
        var fullUri = new Uri("https://storage.blob.core.windows.net/screenshots/test-blob.png");

        // Act
        var blobReference = new BlobReference(containerName, blobName, fullUri);

        // Assert
        Assert.Equal(containerName, blobReference.ContainerName);
        Assert.Equal(blobName, blobReference.BlobName);
        Assert.Equal(fullUri, blobReference.FullUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_InvalidContainerName_ThrowsArgumentException(string? invalidContainerName)
    {
        // Arrange
        var blobName = "test-blob.png";
        var fullUri = new Uri("https://storage.blob.core.windows.net/screenshots/test-blob.png");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new BlobReference(invalidContainerName, blobName, fullUri));
    }

    [Fact]
    public void Equals_SameBlobReference_ReturnsTrue()
    {
        // Arrange
        var containerName = "screenshots";
        var blobName = "test-blob.png";
        var fullUri = new Uri("https://storage.blob.core.windows.net/screenshots/test-blob.png");
        var blobRef1 = new BlobReference(containerName, blobName, fullUri);
        var blobRef2 = new BlobReference(containerName, blobName, fullUri);

        // Act & Assert
        Assert.True(blobRef1.Equals(blobRef2));
        Assert.True(blobRef1 == blobRef2);
        Assert.False(blobRef1 != blobRef2);
    }
}