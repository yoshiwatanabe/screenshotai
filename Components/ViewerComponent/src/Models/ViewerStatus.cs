namespace ViewerComponent.Models;

public class ViewerStatus
{
    public int TotalImages { get; set; }
    public int ImagesWithAnalysis { get; set; }
    public DateTime LastUpdated { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
}