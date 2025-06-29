using System.Drawing;

namespace Storage.Models;

public class ImageOptimizationSettings
{
    public int MaxWidth { get; set; } = 1920;
    public int MaxHeight { get; set; } = 1080;
    public int JpegQuality { get; set; } = 85;
    public bool GenerateThumbnail { get; set; } = true;
    public Size ThumbnailSize { get; set; } = new(300, 200);
}