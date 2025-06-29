namespace Storage.Models;

public class UploadResult
{
    public bool Success { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ThumbnailBlobName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? ErrorMessage { get; set; }
}