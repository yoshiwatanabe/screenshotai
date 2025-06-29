using GalleryViewer.Models;

namespace GalleryViewer.Services;

public interface IGalleryService
{
    /// <summary>
    /// Loads all screenshots from storage
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of screenshot view models</returns>
    Task<List<ScreenshotViewModel>> LoadScreenshotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the gallery by reloading screenshots and updating AI analysis results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated list of screenshot view models</returns>
    Task<List<ScreenshotViewModel>> RefreshGalleryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a screenshot from storage
    /// </summary>
    /// <param name="screenshotId">ID of the screenshot to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeleteScreenshotAsync(Guid screenshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a screenshot
    /// </summary>
    /// <param name="screenshotId">ID of the screenshot to rename</param>
    /// <param name="newName">New display name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully renamed</returns>
    Task<bool> RenameScreenshotAsync(Guid screenshotId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a screenshot
    /// </summary>
    /// <param name="screenshotId">Screenshot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed screenshot information</returns>
    Task<ScreenshotDetailInfo?> GetScreenshotDetailAsync(Guid screenshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches screenshots by text content
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of screenshot view models</returns>
    Task<List<ScreenshotViewModel>> SearchScreenshotsAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets gallery statistics
    /// </summary>
    /// <returns>Gallery statistics</returns>
    Task<GalleryStatistics> GetGalleryStatisticsAsync();

    /// <summary>
    /// Event fired when a new screenshot is added to the gallery
    /// </summary>
    event EventHandler<ScreenshotAddedEventArgs>? ScreenshotAdded;

    /// <summary>
    /// Event fired when a screenshot's analysis is updated
    /// </summary>
    event EventHandler<ScreenshotAnalysisUpdatedEventArgs>? AnalysisUpdated;
}

public class ScreenshotDetailInfo
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string? AiDescription { get; set; }
    public double AiConfidence { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ExtractedText { get; set; }
    public string? AnalysisError { get; set; }
}

public class GalleryStatistics
{
    public int TotalScreenshots { get; set; }
    public int AnalyzedScreenshots { get; set; }
    public int PendingAnalysis { get; set; }
    public int FailedAnalysis { get; set; }
    public long TotalStorageBytes { get; set; }
    public DateTime? LastScreenshotAt { get; set; }
    public DateTime? LastAnalysisAt { get; set; }
}

public class ScreenshotAddedEventArgs : EventArgs
{
    public ScreenshotViewModel Screenshot { get; set; } = new();
}

public class ScreenshotAnalysisUpdatedEventArgs : EventArgs
{
    public Guid ScreenshotId { get; set; }
    public ScreenshotViewModel UpdatedScreenshot { get; set; } = new();
}