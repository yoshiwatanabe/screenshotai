using AzureVisionAnalysis.Models;

namespace AzureVisionAnalysis.Services;

public interface IAnalysisQueueService
{
    /// <summary>
    /// Queues an image for background analysis
    /// </summary>
    /// <param name="screenshotId">ID of the screenshot</param>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The queued analysis job</returns>
    Task<AnalysisJob> QueueAnalysisAsync(Guid screenshotId, string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current queue status
    /// </summary>
    /// <returns>Queue status information</returns>
    Task<AnalysisQueueStatus> GetQueueStatusAsync();

    /// <summary>
    /// Gets analysis results for a screenshot if available
    /// </summary>
    /// <param name="screenshotId">Screenshot ID</param>
    /// <returns>Analysis result if available, null otherwise</returns>
    Task<VisionAnalysisResult?> GetAnalysisResultAsync(Guid screenshotId);

    /// <summary>
    /// Starts the background processing service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartProcessingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background processing service
    /// </summary>
    Task StopProcessingAsync();

    /// <summary>
    /// Gets all completed analysis results
    /// </summary>
    /// <returns>List of completed analysis results with screenshot IDs</returns>
    Task<List<(Guid ScreenshotId, VisionAnalysisResult Result)>> GetAllCompletedAnalysesAsync();

    /// <summary>
    /// Event fired when analysis is completed for a screenshot
    /// </summary>
    event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;
}

public class AnalysisQueueStatus
{
    public int QueuedJobs { get; set; }
    public int ProcessingJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public bool IsProcessing { get; set; }
    public DateTime? LastProcessedAt { get; set; }
}

public class AnalysisCompletedEventArgs : EventArgs
{
    public Guid ScreenshotId { get; set; }
    public VisionAnalysisResult Result { get; set; } = new();
    public bool Success { get; set; }
}