namespace Storage.Exceptions;

public class StorageException : Exception
{
    public StorageErrorCode ErrorCode { get; }
    public string BlobName { get; }

    public StorageException(StorageErrorCode errorCode, string blobName, string message) 
        : base(message)
    {
        ErrorCode = errorCode;
        BlobName = blobName;
    }

    public StorageException(StorageErrorCode errorCode, string blobName, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        BlobName = blobName;
    }
}

public enum StorageErrorCode
{
    ConnectionFailure,
    BlobNotFound,
    InsufficientPermissions,
    QuotaExceeded,
    InvalidImageFormat
}