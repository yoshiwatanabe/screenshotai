using AzureVisionAnalysis.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalleryViewer.Models;
using GalleryViewer.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GalleryViewer.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private readonly IGalleryService _galleryService;
    private readonly IAnalysisQueueService _analysisQueueService;
    private readonly ILogger<GalleryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ScreenshotViewModel> screenshots = new();

    [ObservableProperty]
    private ObservableCollection<ScreenshotViewModel> filteredScreenshots = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ScreenshotViewModel? selectedScreenshot;

    [ObservableProperty]
    private GalleryStatistics? statistics;

    [ObservableProperty]
    private int totalScreenshots;

    [ObservableProperty]
    private int analyzedScreenshots;

    [ObservableProperty]
    private int pendingAnalysis;

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand OpenSelectedCommand { get; }

    public GalleryViewModel(
        IGalleryService galleryService,
        IAnalysisQueueService analysisQueueService,
        ILogger<GalleryViewModel> logger)
    {
        _galleryService = galleryService;
        _analysisQueueService = analysisQueueService;
        _logger = logger;

        // Initialize commands
        LoadCommand = new AsyncRelayCommand(LoadScreenshotsAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshGalleryAsync);
        SearchCommand = new AsyncRelayCommand(PerformSearchAsync);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedScreenshotAsync, () => SelectedScreenshot != null);
        OpenSelectedCommand = new RelayCommand(OpenSelectedScreenshot, () => SelectedScreenshot != null);

        // Subscribe to gallery events
        _galleryService.ScreenshotAdded += OnScreenshotAdded;
        _galleryService.AnalysisUpdated += OnAnalysisUpdated;

        // Watch for search text changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchText))
            {
                SearchCommand.Execute(null);
            }
            if (e.PropertyName == nameof(SelectedScreenshot))
            {
                ((RelayCommand)DeleteSelectedCommand).NotifyCanExecuteChanged();
                ((RelayCommand)OpenSelectedCommand).NotifyCanExecuteChanged();
            }
        };
    }

    public async Task InitializeAsync()
    {
        _logger.LogDebug("Initializing gallery view model");
        await LoadScreenshotsAsync();
        await UpdateStatisticsAsync();
    }

    private async Task LoadScreenshotsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading screenshots...";

            _logger.LogDebug("Loading screenshots");
            var screenshots = await _galleryService.LoadScreenshotsAsync();

            Screenshots.Clear();
            foreach (var screenshot in screenshots)
            {
                // Wire up events for individual screenshot actions
                screenshot.OnDeleteRequested += OnScreenshotDeleteRequested;
                screenshot.OnRenameRequested += OnScreenshotRenameRequested;
                Screenshots.Add(screenshot);
            }

            // Apply current search filter
            await ApplySearchFilterAsync();
            await UpdateStatisticsAsync();

            StatusMessage = $"Loaded {Screenshots.Count} screenshots";
            _logger.LogInformation("Loaded {Count} screenshots", Screenshots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading screenshots");
            StatusMessage = "Error loading screenshots";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshGalleryAsync()
    {
        try
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing gallery...";

            _logger.LogDebug("Refreshing gallery");
            var screenshots = await _galleryService.RefreshGalleryAsync();

            Screenshots.Clear();
            foreach (var screenshot in screenshots)
            {
                screenshot.OnDeleteRequested += OnScreenshotDeleteRequested;
                screenshot.OnRenameRequested += OnScreenshotRenameRequested;
                Screenshots.Add(screenshot);
            }

            await ApplySearchFilterAsync();
            await UpdateStatisticsAsync();

            StatusMessage = $"Refreshed - {Screenshots.Count} screenshots";
            _logger.LogInformation("Refreshed gallery with {Count} screenshots", Screenshots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing gallery");
            StatusMessage = "Error refreshing gallery";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task PerformSearchAsync()
    {
        try
        {
            StatusMessage = string.IsNullOrWhiteSpace(SearchText) ? "Showing all screenshots" : $"Searching for '{SearchText}'...";
            
            await ApplySearchFilterAsync();
            
            StatusMessage = string.IsNullOrWhiteSpace(SearchText) 
                ? $"Showing {FilteredScreenshots.Count} screenshots"
                : $"Found {FilteredScreenshots.Count} results for '{SearchText}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            StatusMessage = "Error performing search";
        }
    }

    private async Task ApplySearchFilterAsync()
    {
        try
        {
            List<ScreenshotViewModel> filteredResults;
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                filteredResults = Screenshots.ToList();
            }
            else
            {
                var searchLower = SearchText.ToLowerInvariant();
                filteredResults = Screenshots.Where(s =>
                    s.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                    (s.AiDescription?.ToLowerInvariant().Contains(searchLower) == true) ||
                    (s.ExtractedText?.ToLowerInvariant().Contains(searchLower) == true) ||
                    s.Tags.Any(tag => tag.ToLowerInvariant().Contains(searchLower))
                ).ToList();
            }

            FilteredScreenshots.Clear();
            foreach (var screenshot in filteredResults.OrderByDescending(s => s.CreatedAt))
            {
                FilteredScreenshots.Add(screenshot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying search filter");
        }
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private async Task DeleteSelectedScreenshotAsync()
    {
        if (SelectedScreenshot == null)
            return;

        await DeleteScreenshotAsync(SelectedScreenshot);
    }

    private void OpenSelectedScreenshot()
    {
        if (SelectedScreenshot == null)
            return;

        SelectedScreenshot.OpenImageCommand.Execute(null);
    }

    private async Task DeleteScreenshotAsync(ScreenshotViewModel screenshot)
    {
        try
        {
            _logger.LogDebug("Deleting screenshot {ScreenshotId}", screenshot.Id);
            
            var success = await _galleryService.DeleteScreenshotAsync(screenshot.Id);
            
            if (success)
            {
                Screenshots.Remove(screenshot);
                FilteredScreenshots.Remove(screenshot);
                
                if (SelectedScreenshot == screenshot)
                {
                    SelectedScreenshot = null;
                }
                
                await UpdateStatisticsAsync();
                StatusMessage = "Screenshot deleted successfully";
                _logger.LogInformation("Deleted screenshot {ScreenshotId}", screenshot.Id);
            }
            else
            {
                StatusMessage = "Failed to delete screenshot";
                _logger.LogWarning("Failed to delete screenshot {ScreenshotId}", screenshot.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting screenshot {ScreenshotId}", screenshot.Id);
            StatusMessage = "Error deleting screenshot";
        }
    }

    private async Task RenameScreenshotAsync(ScreenshotViewModel screenshot, string newName)
    {
        try
        {
            _logger.LogDebug("Renaming screenshot {ScreenshotId} to {NewName}", screenshot.Id, newName);
            
            var success = await _galleryService.RenameScreenshotAsync(screenshot.Id, newName);
            
            if (success)
            {
                screenshot.DisplayName = newName;
                StatusMessage = "Screenshot renamed successfully";
                _logger.LogInformation("Renamed screenshot {ScreenshotId}", screenshot.Id);
            }
            else
            {
                StatusMessage = "Failed to rename screenshot";
                _logger.LogWarning("Failed to rename screenshot {ScreenshotId}", screenshot.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming screenshot {ScreenshotId}", screenshot.Id);
            StatusMessage = "Error renaming screenshot";
        }
    }

    private async Task UpdateStatisticsAsync()
    {
        try
        {
            Statistics = await _galleryService.GetGalleryStatisticsAsync();
            TotalScreenshots = Statistics.TotalScreenshots;
            AnalyzedScreenshots = Statistics.AnalyzedScreenshots;
            PendingAnalysis = Statistics.PendingAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statistics");
        }
    }

    private void OnScreenshotAdded(object? sender, ScreenshotAddedEventArgs e)
    {
        try
        {
            _logger.LogDebug("New screenshot added to gallery: {ScreenshotId}", e.Screenshot.Id);
            
            // Add to main collection
            e.Screenshot.OnDeleteRequested += OnScreenshotDeleteRequested;
            e.Screenshot.OnRenameRequested += OnScreenshotRenameRequested;
            Screenshots.Insert(0, e.Screenshot); // Add at beginning for newest first
            
            // Update filtered collection if it matches search
            Task.Run(async () => await ApplySearchFilterAsync());
            Task.Run(async () => await UpdateStatisticsAsync());
            
            StatusMessage = "New screenshot added";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling screenshot added event");
        }
    }

    private void OnAnalysisUpdated(object? sender, ScreenshotAnalysisUpdatedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Analysis updated for screenshot: {ScreenshotId}", e.ScreenshotId);
            
            var existingScreenshot = Screenshots.FirstOrDefault(s => s.Id == e.ScreenshotId);
            if (existingScreenshot != null)
            {
                // Update the existing view model with new analysis data
                existingScreenshot.AiDescription = e.UpdatedScreenshot.AiDescription;
                existingScreenshot.AiConfidence = e.UpdatedScreenshot.AiConfidence;
                existingScreenshot.HasAiAnalysis = e.UpdatedScreenshot.HasAiAnalysis;
                existingScreenshot.IsAnalyzing = e.UpdatedScreenshot.IsAnalyzing;
                existingScreenshot.AnalysisError = e.UpdatedScreenshot.AnalysisError;
                existingScreenshot.Tags = e.UpdatedScreenshot.Tags;
                existingScreenshot.ExtractedText = e.UpdatedScreenshot.ExtractedText;
                
                Task.Run(async () => await UpdateStatisticsAsync());
                StatusMessage = "Analysis completed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling analysis updated event");
        }
    }

    private async void OnScreenshotDeleteRequested(ScreenshotViewModel screenshot)
    {
        await DeleteScreenshotAsync(screenshot);
    }

    private async void OnScreenshotRenameRequested(ScreenshotViewModel screenshot)
    {
        // In a real app, this would show a rename dialog
        // For now, we'll just log it
        _logger.LogDebug("Rename requested for screenshot {ScreenshotId}", screenshot.Id);
        StatusMessage = "Rename functionality not implemented in demo";
    }
}