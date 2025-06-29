using System.Drawing;

namespace ScreenshotCapture.Services;

public interface IAreaSelectionService
{
    /// <summary>
    /// Shows an overlay for the user to select a screen area
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to abort selection</param>
    /// <returns>The selected rectangle, or Rectangle.Empty if cancelled</returns>
    Task<Rectangle> ShowAreaSelectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the bounds of all available screens
    /// </summary>
    /// <returns>Combined bounds of all screens</returns>
    Rectangle GetVirtualScreenBounds();

    /// <summary>
    /// Checks if area selection is currently active
    /// </summary>
    bool IsSelectionActive { get; }
}

public class AreaSelectionCancelledException : OperationCanceledException
{
    public AreaSelectionCancelledException() : base("Area selection was cancelled by user") { }
}