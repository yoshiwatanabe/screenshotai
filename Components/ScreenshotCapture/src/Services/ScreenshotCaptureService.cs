using Microsoft.Extensions.Logging;
using ScreenshotCapture.Models;
using Storage.Services;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenshotCapture.Services;

public class ScreenshotCaptureService : IScreenshotCaptureService
{
    private readonly IAreaSelectionService _areaSelectionService;
    private readonly ILocalStorageService _storageService;
    private readonly ILogger<ScreenshotCaptureService> _logger;

    public ScreenshotCaptureService(
        IAreaSelectionService areaSelectionService,
        ILocalStorageService storageService,
        ILogger<ScreenshotCaptureService> logger)
    {
        _areaSelectionService = areaSelectionService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<CaptureResult> CaptureAreaAsync(Rectangle area, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Capturing area: {Width}x{Height} at ({X}, {Y})", 
                area.Width, area.Height, area.X, area.Y);

            if (area.Width <= 0 || area.Height <= 0)
            {
                return CaptureResult.CreateFailure("Invalid capture area dimensions");
            }

            // Capture the screen area
            var imageData = await CaptureScreenAreaAsync(area, cancellationToken);
            
            if (imageData == null || imageData.Length == 0)
            {
                return CaptureResult.CreateFailure("Failed to capture screen area");
            }

            var fileName = GenerateScreenshotFilename();
            
            stopwatch.Stop();
            _logger.LogDebug("Screen capture completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return CaptureResult.CreateSuccess(imageData, fileName, area, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Screen capture was cancelled");
            return CaptureResult.CreateFailure("Capture operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screen area");
            return CaptureResult.CreateFailure($"Capture failed: {ex.Message}");
        }
    }

    public async Task<CaptureResult> CaptureWithAreaSelectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting capture with area selection");
            
            // Show area selection overlay
            var selectedArea = await _areaSelectionService.ShowAreaSelectionAsync(cancellationToken);
            
            if (selectedArea.IsEmpty)
            {
                return CaptureResult.CreateFailure("No area selected");
            }

            // Capture the selected area
            return await CaptureAreaAsync(selectedArea, cancellationToken);
        }
        catch (AreaSelectionCancelledException)
        {
            _logger.LogDebug("Area selection was cancelled by user");
            return CaptureResult.CreateFailure("Area selection was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in capture with area selection");
            return CaptureResult.CreateFailure($"Capture with area selection failed: {ex.Message}");
        }
    }

    public async Task<CaptureResult> PerformCompleteCaptureWorkflowAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting complete capture workflow");
            
            // 1. Capture with area selection
            var captureResult = await CaptureWithAreaSelectionAsync(cancellationToken);
            
            if (!captureResult.Success || captureResult.ImageData == null)
            {
                return captureResult;
            }

            // 2. Save to local storage
            var uploadResult = await _storageService.UploadFromClipboardAsync(
                captureResult.ImageData, 
                captureResult.FileName!, 
                cancellationToken);

            if (!uploadResult.Success)
            {
                _logger.LogError("Failed to save screenshot: {Error}", uploadResult.ErrorMessage);
                return CaptureResult.CreateFailure($"Failed to save screenshot: {uploadResult.ErrorMessage}");
            }

            // 3. Update result with storage information
            captureResult.FileName = uploadResult.BlobName;
            stopwatch.Stop();
            captureResult.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation("Complete capture workflow finished successfully in {Duration}ms. File: {FileName}", 
                stopwatch.ElapsedMilliseconds, captureResult.FileName);

            return captureResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complete capture workflow");
            return CaptureResult.CreateFailure($"Complete capture workflow failed: {ex.Message}");
        }
    }

    public string GenerateScreenshotFilename(DateTime? timestamp = null)
    {
        var time = timestamp ?? DateTime.Now;
        return $"screenshot_{time:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[0..8]}.png";
    }

    private async Task<byte[]?> CaptureScreenAreaAsync(Rectangle area, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(bitmap);
                
                // Capture the screen area
                graphics.CopyFromScreen(area.X, area.Y, 0, 0, area.Size, CopyPixelOperation.SourceCopy);
                
                // Convert to byte array
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screen area");
                return null;
            }
        }, cancellationToken);
    }
}