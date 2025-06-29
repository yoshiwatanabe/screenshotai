using AzureVisionAnalysis.Models;

namespace AzureVisionAnalysis.Services;

public interface IAzureVisionService
{
    /// <summary>
    /// Analyzes an image file and returns comprehensive analysis results
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vision analysis result</returns>
    Task<VisionAnalysisResult> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes image data and returns comprehensive analysis results
    /// </summary>
    /// <param name="imageData">Image data as byte array</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vision analysis result</returns>
    Task<VisionAnalysisResult> AnalyzeImageAsync(byte[] imageData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets just the main caption for an image (faster, less detailed)
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Main caption with confidence score</returns>
    Task<(string caption, double confidence)> GetImageCaptionAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Azure Vision service is configured and available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is available</returns>
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an image file is suitable for analysis
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>True if image can be analyzed</returns>
    Task<bool> CanAnalyzeImageAsync(string imagePath);
}