namespace ViewerComponent.Models;

public class ImageFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string? JsonPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public long FileSize { get; set; }
    public bool HasAnalysis => !string.IsNullOrEmpty(JsonPath);
}