namespace AzureVisionAnalysis.Models;

public class VisionAnalysisResult
{
    public bool Success { get; set; }
    public string? MainCaption { get; set; }
    public double MainCaptionConfidence { get; set; }
    public List<string> DenseCaptions { get; set; } = new();
    public List<DetectedObject> Objects { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? ExtractedText { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    public static VisionAnalysisResult CreateSuccess(
        string mainCaption, 
        double confidence,
        List<string>? denseCaptions = null,
        List<DetectedObject>? objects = null,
        List<string>? tags = null,
        string? extractedText = null)
    {
        return new VisionAnalysisResult
        {
            Success = true,
            MainCaption = mainCaption,
            MainCaptionConfidence = confidence,
            DenseCaptions = denseCaptions ?? new(),
            Objects = objects ?? new(),
            Tags = tags ?? new(),
            ExtractedText = extractedText
        };
    }

    public static VisionAnalysisResult CreateFailure(string errorMessage)
    {
        return new VisionAnalysisResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Gets a comprehensive description combining main caption and dense captions
    /// </summary>
    public string GetComprehensiveDescription()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(MainCaption))
        {
            parts.Add($"Main: {MainCaption}");
        }

        if (DenseCaptions.Any())
        {
            parts.Add($"Details: {string.Join(", ", DenseCaptions.Take(3))}");
        }

        if (Objects.Any())
        {
            var objectNames = Objects.Where(o => o.Confidence > 0.7)
                                   .Select(o => o.Name)
                                   .Take(5);
            if (objectNames.Any())
            {
                parts.Add($"Objects: {string.Join(", ", objectNames)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(ExtractedText) && ExtractedText.Length > 0)
        {
            var textPreview = ExtractedText.Length > 100 
                ? ExtractedText[..100] + "..." 
                : ExtractedText;
            parts.Add($"Text: {textPreview}");
        }

        return parts.Any() ? string.Join(" | ", parts) : "Analysis completed";
    }
}

public class DetectedObject
{
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}