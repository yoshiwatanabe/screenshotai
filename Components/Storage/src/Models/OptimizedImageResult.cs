namespace Storage.Models;

public class OptimizedImageResult
{
    public byte[] OptimizedImageData { get; set; } = [];
    public byte[] ThumbnailData { get; set; } = [];
    public long OriginalSize { get; set; }
    public long OptimizedSize { get; set; }
    public double CompressionRatio { get; set; }
}