using System.Drawing;

namespace ScreenshotCapture.Models;

public class CaptureRequest
{
    public Rectangle CaptureArea { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? PreferredFileName { get; set; }
}

public class CaptureResult
{
    public bool Success { get; set; }
    public byte[]? ImageData { get; set; }
    public string? FileName { get; set; }
    public Rectangle CapturedArea { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }

    public static CaptureResult CreateSuccess(byte[] imageData, string fileName, Rectangle area, TimeSpan processingTime)
    {
        return new CaptureResult
        {
            Success = true,
            ImageData = imageData,
            FileName = fileName,
            CapturedArea = area,
            CapturedAt = DateTime.UtcNow,
            ProcessingTime = processingTime
        };
    }

    public static CaptureResult CreateFailure(string errorMessage)
    {
        return new CaptureResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            CapturedAt = DateTime.UtcNow
        };
    }
}