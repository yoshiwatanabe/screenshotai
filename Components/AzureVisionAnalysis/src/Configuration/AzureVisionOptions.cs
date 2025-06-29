namespace AzureVisionAnalysis.Configuration;

public class AzureVisionOptions
{
    public const string SectionName = "AzureVision";

    /// <summary>
    /// Azure AI Vision endpoint URL (from Azure AI Foundry)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Vision API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model deployment name (if using Azure AI Foundry custom deployment)
    /// </summary>
    public string? ModelDeploymentName { get; set; }

    /// <summary>
    /// API version to use
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>
    /// Timeout for API requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum file size for analysis in bytes (default 20MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Whether to include dense captions in analysis
    /// </summary>
    public bool IncludeDenseCaptions { get; set; } = true;

    /// <summary>
    /// Whether to include object detection
    /// </summary>
    public bool IncludeObjects { get; set; } = true;

    /// <summary>
    /// Whether to include text extraction (OCR)
    /// </summary>
    public bool IncludeText { get; set; } = true;

    /// <summary>
    /// Whether to include tag generation
    /// </summary>
    public bool IncludeTags { get; set; } = true;

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Azure Vision endpoint is required");

        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("Azure Vision API key is required");

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            throw new InvalidOperationException("Azure Vision endpoint must be a valid URL");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Timeout must be greater than 0");

        if (MaxRetryAttempts < 0)
            throw new InvalidOperationException("Max retry attempts cannot be negative");

        if (MaxFileSizeBytes <= 0)
            throw new InvalidOperationException("Max file size must be greater than 0");
    }
}