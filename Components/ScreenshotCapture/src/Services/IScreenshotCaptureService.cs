using ScreenshotCapture.Models;
using System.Drawing;

namespace ScreenshotCapture.Services;

public interface IScreenshotCaptureService
{
    /// <summary>
    /// Captures a screenshot of the specified area
    /// </summary>
    /// <param name="area">Area to capture</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capture result with image data</returns>
    Task<CaptureResult> CaptureAreaAsync(Rectangle area, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows area selection overlay and captures the selected area
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capture result with image data</returns>
    Task<CaptureResult> CaptureWithAreaSelectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates the complete capture workflow (hotkey -> area selection -> capture -> save)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capture result with file information</returns>
    Task<CaptureResult> PerformCompleteCaptureWorkflowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique filename for a screenshot
    /// </summary>
    /// <param name="timestamp">Timestamp for the screenshot</param>
    /// <returns>Generated filename</returns>
    string GenerateScreenshotFilename(DateTime? timestamp = null);
}