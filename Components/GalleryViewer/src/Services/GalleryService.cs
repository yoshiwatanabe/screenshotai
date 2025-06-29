using AzureVisionAnalysis.Services;
using Domain.Entities;
using Domain.Enums;
using GalleryViewer.Models;
using Microsoft.Extensions.Logging;
using Storage.Services;
using System.Drawing;

namespace GalleryViewer.Services;

public class GalleryService : IGalleryService
{
    private readonly ILocalStorageService _storageService;
    private readonly IAnalysisQueueService _analysisQueueService;
    private readonly ILogger<GalleryService> _logger;
    
    // In-memory storage for screenshots (in real app, this would be a database)
    private readonly List<Screenshot> _screenshots = new();
    private readonly object _screenshotsLock = new();

    public event EventHandler<ScreenshotAddedEventArgs>? ScreenshotAdded;
    public event EventHandler<ScreenshotAnalysisUpdatedEventArgs>? AnalysisUpdated;

    public GalleryService(
        ILocalStorageService storageService,
        IAnalysisQueueService analysisQueueService,
        ILogger<GalleryService> logger)
    {
        _storageService = storageService;
        _analysisQueueService = analysisQueueService;
        _logger = logger;

        // Subscribe to analysis completion events
        _analysisQueueService.AnalysisCompleted += OnAnalysisCompleted;
    }

    public async Task<List<ScreenshotViewModel>> LoadScreenshotsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Loading screenshots from storage");
            
            // For demo purposes, we'll scan the screenshots directory
            // In a real app, this would come from a database
            await LoadScreenshotsFromDirectoryAsync(cancellationToken);
            
            var viewModels = new List<ScreenshotViewModel>();
            
            lock (_screenshotsLock)
            {
                foreach (var screenshot in _screenshots.OrderByDescending(s => s.CreatedAt))
                {
                    var imagePath = await _storageService.GetImagePathAsync(screenshot.BlobName);
                    var thumbnailPath = await _storageService.GetThumbnailPathAsync(screenshot.BlobName);
                    
                    var viewModel = ScreenshotViewModel.FromScreenshot(screenshot, imagePath, thumbnailPath);
                    
                    // Check for existing analysis results
                    var analysisResult = await _analysisQueueService.GetAnalysisResultAsync(screenshot.Id);
                    if (analysisResult != null)
                    {
                        viewModel.UpdateAnalysisResult(analysisResult);
                    }
                    
                    viewModels.Add(viewModel);
                }
            }
            
            _logger.LogInformation("Loaded {Count} screenshots", viewModels.Count);
            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading screenshots");
            return new List<ScreenshotViewModel>();
        }
    }

    public async Task<List<ScreenshotViewModel>> RefreshGalleryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing gallery");
        return await LoadScreenshotsAsync(cancellationToken);
    }

    public async Task<bool> DeleteScreenshotAsync(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting screenshot {ScreenshotId}", screenshotId);
            
            Screenshot? screenshot;
            lock (_screenshotsLock)
            {
                screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
                if (screenshot == null)
                {
                    _logger.LogWarning("Screenshot not found for deletion: {ScreenshotId}", screenshotId);
                    return false;
                }
            }
            
            // Delete from storage
            var deleted = await _storageService.DeleteImageAsync(screenshot.BlobName, cancellationToken);
            
            if (deleted)
            {
                lock (_screenshotsLock)
                {
                    _screenshots.Remove(screenshot);
                }
                
                _logger.LogInformation("Successfully deleted screenshot {ScreenshotId}", screenshotId);
            }
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting screenshot {ScreenshotId}", screenshotId);
            return false;
        }
    }

    public async Task<bool> RenameScreenshotAsync(Guid screenshotId, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Renaming screenshot {ScreenshotId} to {NewName}", screenshotId, newName);
            
            lock (_screenshotsLock)
            {
                var screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
                if (screenshot == null)
                {
                    _logger.LogWarning("Screenshot not found for rename: {ScreenshotId}", screenshotId);
                    return false;
                }
                
                screenshot.UpdateDisplayName(newName);
            }
            
            _logger.LogInformation("Successfully renamed screenshot {ScreenshotId}", screenshotId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming screenshot {ScreenshotId}", screenshotId);
            return false;
        }
    }

    public async Task<ScreenshotDetailInfo?> GetScreenshotDetailAsync(Guid screenshotId, CancellationToken cancellationToken = default)
    {
        try
        {
            Screenshot? screenshot;
            lock (_screenshotsLock)
            {
                screenshot = _screenshots.FirstOrDefault(s => s.Id == screenshotId);
                if (screenshot == null)
                    return null;
            }
            
            var imagePath = await _storageService.GetImagePathAsync(screenshot.BlobName);
            var analysisResult = await _analysisQueueService.GetAnalysisResultAsync(screenshotId);
            
            var detail = new ScreenshotDetailInfo
            {
                Id = screenshot.Id,
                DisplayName = screenshot.DisplayName,
                ImagePath = imagePath,
                CreatedAt = screenshot.CreatedAt,
                FileSizeBytes = File.Exists(imagePath) ? new FileInfo(imagePath).Length : 0
            };
            
            // Get image dimensions
            if (File.Exists(imagePath))
            {
                try
                {
                    using var image = Image.FromFile(imagePath);
                    detail.ImageWidth = image.Width;
                    detail.ImageHeight = image.Height;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read image dimensions for {ImagePath}", imagePath);
                }
            }
            
            // Add analysis results if available
            if (analysisResult != null)
            {
                if (analysisResult.Success)
                {
                    detail.AiDescription = analysisResult.GetComprehensiveDescription();
                    detail.AiConfidence = analysisResult.MainCaptionConfidence;
                    detail.Tags = analysisResult.Tags;
                    detail.ExtractedText = analysisResult.ExtractedText;
                }
                else
                {
                    detail.AnalysisError = analysisResult.ErrorMessage;
                }
            }
            
            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting screenshot detail for {ScreenshotId}", screenshotId);
            return null;
        }
    }

    public async Task<List<ScreenshotViewModel>> SearchScreenshotsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return await LoadScreenshotsAsync(cancellationToken);
        }
        
        try
        {
            _logger.LogDebug("Searching screenshots for: {SearchText}", searchText);
            
            var allScreenshots = await LoadScreenshotsAsync(cancellationToken);
            var searchLower = searchText.ToLowerInvariant();
            
            var filtered = allScreenshots.Where(s =>
                s.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                (s.AiDescription?.ToLowerInvariant().Contains(searchLower) == true) ||
                (s.ExtractedText?.ToLowerInvariant().Contains(searchLower) == true) ||
                s.Tags.Any(tag => tag.ToLowerInvariant().Contains(searchLower))
            ).ToList();
            
            _logger.LogDebug("Found {Count} screenshots matching search", filtered.Count);
            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching screenshots");
            return new List<ScreenshotViewModel>();
        }
    }

    public async Task<GalleryStatistics> GetGalleryStatisticsAsync()
    {
        try
        {
            var screenshots = await LoadScreenshotsAsync();
            var queueStatus = await _analysisQueueService.GetQueueStatusAsync();
            
            var stats = new GalleryStatistics
            {
                TotalScreenshots = screenshots.Count,
                AnalyzedScreenshots = screenshots.Count(s => s.HasAiAnalysis),
                PendingAnalysis = queueStatus.QueuedJobs + queueStatus.ProcessingJobs,
                FailedAnalysis = screenshots.Count(s => !string.IsNullOrEmpty(s.AnalysisError)),
                TotalStorageBytes = screenshots.Sum(s => s.FileSizeBytes),
                LastScreenshotAt = screenshots.Any() ? screenshots.Max(s => s.CreatedAt) : null,
                LastAnalysisAt = queueStatus.LastProcessedAt
            };
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gallery statistics");
            return new GalleryStatistics();
        }
    }

    public async Task AddScreenshotAsync(Screenshot screenshot, string imagePath)
    {
        try
        {
            lock (_screenshotsLock)
            {
                _screenshots.Add(screenshot);
            }
            
            var thumbnailPath = await _storageService.GetThumbnailPathAsync(screenshot.BlobName);
            var viewModel = ScreenshotViewModel.FromScreenshot(screenshot, imagePath, thumbnailPath);
            
            _logger.LogInformation("Added new screenshot to gallery: {ScreenshotId}", screenshot.Id);
            
            ScreenshotAdded?.Invoke(this, new ScreenshotAddedEventArgs { Screenshot = viewModel });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding screenshot to gallery: {ScreenshotId}", screenshot.Id);
        }
    }

    private async Task LoadScreenshotsFromDirectoryAsync(CancellationToken cancellationToken)
    {
        // This is a simplified implementation
        // In a real app, screenshots would be stored in a database
        
        // For now, we'll create some mock data if the list is empty
        lock (_screenshotsLock)
        {
            if (_screenshots.Any())
                return; // Already loaded
        }
        
        _logger.LogDebug("Initializing screenshot collection (demo mode)");
        
        // In a real implementation, this would load from a database
        // For demo purposes, we're starting with an empty collection
    }

    private void OnAnalysisCompleted(object? sender, AzureVisionAnalysis.Services.AnalysisCompletedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Analysis completed for screenshot {ScreenshotId}, updating gallery", e.ScreenshotId);
            
            Screenshot? screenshot;
            lock (_screenshotsLock)
            {
                screenshot = _screenshots.FirstOrDefault(s => s.Id == e.ScreenshotId);
            }
            
            if (screenshot != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var imagePath = await _storageService.GetImagePathAsync(screenshot.BlobName);
                        var thumbnailPath = await _storageService.GetThumbnailPathAsync(screenshot.BlobName);
                        var viewModel = ScreenshotViewModel.FromScreenshot(screenshot, imagePath, thumbnailPath);
                        
                        if (e.Success)
                        {
                            viewModel.UpdateAnalysisResult(e.Result);
                        }
                        else
                        {
                            viewModel.AnalysisError = e.Result.ErrorMessage;
                        }
                        
                        AnalysisUpdated?.Invoke(this, new ScreenshotAnalysisUpdatedEventArgs
                        {
                            ScreenshotId = e.ScreenshotId,
                            UpdatedScreenshot = viewModel
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating gallery view model for screenshot {ScreenshotId}", e.ScreenshotId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling analysis completion for screenshot {ScreenshotId}", e.ScreenshotId);
        }
    }
}