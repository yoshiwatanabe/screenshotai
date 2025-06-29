using Storage.Exceptions;
using Xunit;

namespace StorageTests;

public class StorageExceptionTests
{
    [Fact]
    public void Constructor_WithErrorCodeBlobNameAndMessage_SetsProperties()
    {
        var errorCode = StorageErrorCode.BlobNotFound;
        var blobName = "test-blob.jpg";
        var message = "Test error message";

        var exception = new StorageException(errorCode, blobName, message);

        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(blobName, exception.BlobName);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithInnerException_SetsAllProperties()
    {
        var errorCode = StorageErrorCode.ConnectionFailure;
        var blobName = "test-blob.jpg";
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner exception");

        var exception = new StorageException(errorCode, blobName, message, innerException);

        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(blobName, exception.BlobName);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(StorageErrorCode.ConnectionFailure)]
    [InlineData(StorageErrorCode.BlobNotFound)]
    [InlineData(StorageErrorCode.InsufficientPermissions)]
    [InlineData(StorageErrorCode.QuotaExceeded)]
    [InlineData(StorageErrorCode.InvalidImageFormat)]
    public void StorageErrorCode_AllValues_AreValid(StorageErrorCode errorCode)
    {
        Assert.True(Enum.IsDefined(typeof(StorageErrorCode), errorCode));
    }

    [Fact]
    public void StorageErrorCode_HasExpectedValues()
    {
        var expectedValues = new[]
        {
            StorageErrorCode.ConnectionFailure,
            StorageErrorCode.BlobNotFound,
            StorageErrorCode.InsufficientPermissions,
            StorageErrorCode.QuotaExceeded,
            StorageErrorCode.InvalidImageFormat
        };

        var actualValues = Enum.GetValues<StorageErrorCode>();

        Assert.Equal(expectedValues.Length, actualValues.Length);
        Assert.All(expectedValues, expected => Assert.Contains(expected, actualValues));
    }
}