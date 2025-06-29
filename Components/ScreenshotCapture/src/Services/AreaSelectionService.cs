using Microsoft.Extensions.Logging;
using ScreenshotCapture.UI;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotCapture.Services;

public class AreaSelectionService : IAreaSelectionService
{
    private readonly ILogger<AreaSelectionService> _logger;
    private bool _isSelectionActive;
    
    public bool IsSelectionActive => _isSelectionActive;

    public AreaSelectionService(ILogger<AreaSelectionService> logger)
    {
        _logger = logger;
    }

    public async Task<Rectangle> ShowAreaSelectionAsync(CancellationToken cancellationToken = default)
    {
        if (_isSelectionActive)
        {
            _logger.LogWarning("Area selection is already active");
            throw new InvalidOperationException("Area selection is already in progress");
        }

        try
        {
            _isSelectionActive = true;
            _logger.LogDebug("Starting area selection");

            using var overlay = new AreaSelectionOverlay();
            
            // Handle cancellation
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    overlay.BeginInvoke(new Action(() => overlay.Close()));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing overlay on cancellation");
                }
            });

            var selectedArea = await overlay.ShowSelectionAsync();
            
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Area selection was cancelled");
                throw new AreaSelectionCancelledException();
            }

            if (selectedArea.IsEmpty || selectedArea.Width < 10 || selectedArea.Height < 10)
            {
                _logger.LogDebug("Area selection was cancelled or too small");
                throw new AreaSelectionCancelledException();
            }

            _logger.LogDebug("Area selected: {Width}x{Height} at ({X}, {Y})", 
                selectedArea.Width, selectedArea.Height, selectedArea.X, selectedArea.Y);
            
            return selectedArea;
        }
        catch (AreaSelectionCancelledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during area selection");
            throw new InvalidOperationException("Failed to perform area selection", ex);
        }
        finally
        {
            _isSelectionActive = false;
        }
    }

    public Rectangle GetVirtualScreenBounds()
    {
        return new Rectangle(
            SystemInformation.VirtualScreen.Left,
            SystemInformation.VirtualScreen.Top,
            SystemInformation.VirtualScreen.Width,
            SystemInformation.VirtualScreen.Height);
    }
}