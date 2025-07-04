namespace Vision.Configuration;

public class AzureVisionOptions
{
    public bool Enabled { get; set; } = false;
    public bool Simulate { get; set; } = false;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string Language { get; set; } = "en";
    public bool GenderNeutralCaption { get; set; } = true;
    public List<string> Features { get; set; } = new() { "read", "tags", "objects", "people" };
    public double MinConfidenceThreshold { get; set; } = 0.5;
}