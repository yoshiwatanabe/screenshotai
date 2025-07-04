using Storage.Models;
using Storage.Services;

namespace Storage.Configuration;

public class StorageOptions
{
    public string ScreenshotsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Screenshots");
    public string ThumbnailsDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Screenshots", "Thumbnails");
    public ImageOptimizationSettings DefaultOptimization { get; set; } = new();
    public bool CreateDirectoriesIfNotExist { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
    
}