# GalleryViewer Component

## Responsibility

Provides a WPF-based gallery interface for viewing, managing, and searching screenshots with real-time AI analysis updates.

## Key Features

- **Grid-Based Gallery**: Responsive tile layout showing screenshot thumbnails
- **Real-Time Updates**: Automatically updates when new screenshots are captured or analyzed
- **Search Functionality**: Search by filename, AI description, extracted text, or tags
- **Interactive Actions**: Open, delete, rename, and view in explorer
- **Analysis Status**: Visual indicators for analysis progress and results
- **Keyboard Shortcuts**: F5 (refresh), Delete (remove), Enter (open), Ctrl+F (search)
- **Statistics Display**: Total, analyzed, and pending analysis counts

## Public Interface

### Core Services

```csharp
// Gallery management service
services.AddSingleton<IGalleryService, GalleryService>();

// View model for gallery
services.AddTransient<GalleryViewModel>();

// WPF gallery window
services.AddTransient<GalleryWindow>();

// Complete service registration
services.AddGalleryViewerServices();
```

### Key Methods

```csharp
// Load and display screenshots
Task<List<ScreenshotViewModel>> LoadScreenshotsAsync();

// Refresh gallery with latest data
Task<List<ScreenshotViewModel>> RefreshGalleryAsync();

// Search screenshots
Task<List<ScreenshotViewModel>> SearchScreenshotsAsync(string searchText);

// Delete screenshot
Task<bool> DeleteScreenshotAsync(Guid screenshotId);

// Get gallery statistics
Task<GalleryStatistics> GetGalleryStatisticsAsync();
```

## Dependencies

### External Dependencies
- `Microsoft.Extensions.Logging.Abstractions` - Logging support
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI support
- `CommunityToolkit.Mvvm` - MVVM framework with ObservableObject and commands
- `Microsoft.Xaml.Behaviors.Wpf` - WPF behaviors for advanced UI interactions

### Internal Dependencies
- `Domain` component - Core entities and models
- `Storage` component - Local file operations
- `AzureVisionAnalysis` component - AI analysis results and events

## Usage Example

```csharp
// Register all services
services.AddLocalStorageServices(configuration);
services.AddAzureVisionAnalysisServices(configuration);
services.AddGalleryViewerServices();

// Use in application
public class ScreenshotApp : Application
{
    private readonly IServiceProvider _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Show gallery window
        var galleryWindow = _serviceProvider.GetRequiredService<GalleryWindow>();
        galleryWindow.Show();
    }
}

// Or integrate with screenshot capture
public class ScreenshotManager
{
    private readonly IGalleryService _galleryService;
    private readonly IScreenshotCaptureService _captureService;
    
    public async Task CaptureAndShowInGallery()
    {
        // Capture screenshot
        var result = await _captureService.PerformCompleteCaptureWorkflowAsync();
        
        // Gallery automatically updates via events
        // No manual refresh needed
    }
}
```

## User Interface

### Main Window Layout
```
┌─────────────────────────────────────────────────────────────┐
│ [🔄 Refresh] [📁 Load]    [Search Box]    [Statistics]     │
├─────────────────────────────────────────────────────────────┤
│ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐         │
│ │ [IMG] │ │ [IMG] │ │ [IMG] │ │ [IMG] │ │ [IMG] │         │
│ │ Name  │ │ Name  │ │ Name  │ │ Name  │ │ Name  │         │
│ │ Date  │ │ Date  │ │ Date  │ │ Date  │ │ Date  │         │
│ │ AI... │ │ AI... │ │ AI... │ │ AI... │ │ AI... │         │
│ │[👁️📁🗑️]│ │[👁️📁🗑️]│ │[👁️📁🗑️]│ │[👁️📁🗑️]│ │[👁️📁🗑️]│         │
│ └───────┘ └───────┘ └───────┘ └───────┘ └───────┘         │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ Status: Ready    Press Ctrl+Print Screen to capture [Help] │
└─────────────────────────────────────────────────────────────┘
```

### Screenshot Tile Features
- **Thumbnail**: 200px height preview of the screenshot
- **Name & Status**: Display name with color-coded status badge
- **Metadata**: Creation date and file size
- **AI Description**: Comprehensive description when available
- **Progress Indicator**: Loading animation during analysis
- **Error Display**: Clear error messages for failed analysis
- **Action Buttons**: 
  - 👁️ Open in default image viewer
  - 📁 Show in file explorer
  - 🗑️ Delete screenshot

## Real-Time Updates

### Event-Driven Architecture
```csharp
// Gallery automatically updates when:
public class GalleryService : IGalleryService
{
    // 1. New screenshot captured
    public event EventHandler<ScreenshotAddedEventArgs>? ScreenshotAdded;
    
    // 2. AI analysis completes
    public event EventHandler<ScreenshotAnalysisUpdatedEventArgs>? AnalysisUpdated;
}

// View model subscribes to events
public class GalleryViewModel : ObservableObject
{
    private void OnAnalysisCompleted(object? sender, AnalysisCompletedEventArgs e)
    {
        // Automatically update UI with new AI description
        var screenshot = Screenshots.FirstOrDefault(s => s.Id == e.ScreenshotId);
        if (screenshot != null)
        {
            screenshot.UpdateAnalysisResult(e.Result);
        }
    }
}
```

## Search Functionality

### Multi-Field Search
```csharp
// Searches across multiple fields
var filteredResults = Screenshots.Where(s =>
    s.DisplayName.ToLowerInvariant().Contains(searchLower) ||           // Filename
    (s.AiDescription?.ToLowerInvariant().Contains(searchLower) == true) || // AI description
    (s.ExtractedText?.ToLowerInvariant().Contains(searchLower) == true) || // OCR text
    s.Tags.Any(tag => tag.ToLowerInvariant().Contains(searchLower))        // Tags
).ToList();
```

### Search Features
- **Real-Time**: Updates as you type
- **Case-Insensitive**: Works regardless of letter case
- **Multi-Field**: Searches name, AI description, OCR text, and tags
- **Clear Function**: ESC key or clear button to reset
- **Result Count**: Shows number of matches found

## Status Indicators

### Visual Status System
- **🟢 Green Badge**: "Ready" - Analysis completed successfully
- **🟠 Orange Badge**: "Analyzing..." - AI analysis in progress
- **🔴 Red Badge**: "Analysis Failed" - Error occurred during analysis
- **⚪ Gray Badge**: "Processing" - Initial screenshot processing

### Progress Indicators
- **Loading Bar**: Animated progress bar during gallery loading
- **Analysis Animation**: Individual screenshot analysis progress
- **Status Messages**: Clear text feedback for all operations

## Keyboard Shortcuts

### Gallery Navigation
- **F5**: Refresh gallery
- **Delete**: Delete selected screenshot
- **Enter**: Open selected screenshot
- **ESC**: Clear search
- **Ctrl+F**: Focus search box (planned)
- **Arrow Keys**: Navigate between screenshots (planned)

## Error Handling

### User-Friendly Error Display
```csharp
// Analysis errors shown in UI
public class ScreenshotViewModel : ObservableObject
{
    public string? AnalysisError { get; set; }  // "Service unavailable"
    public bool HasAnalysisError => !string.IsNullOrEmpty(AnalysisError);
}

// Gallery operation errors
public class GalleryViewModel : ObservableObject
{
    public string StatusMessage { get; set; }  // "Error loading screenshots"
    
    private async Task DeleteScreenshotAsync(ScreenshotViewModel screenshot)
    {
        try
        {
            var success = await _galleryService.DeleteScreenshotAsync(screenshot.Id);
            StatusMessage = success ? "Screenshot deleted" : "Failed to delete";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error deleting screenshot";
            _logger.LogError(ex, "Delete failed");
        }
    }
}
```

## Performance Optimizations

### Efficient Loading
- **Thumbnail Generation**: Fast preview images for quick loading
- **Lazy Loading**: Load full images only when needed
- **Virtual Scrolling**: Handle large collections efficiently (planned)
- **Background Processing**: Non-blocking AI analysis

### Memory Management
- **Image Disposal**: Proper cleanup of image resources
- **Observable Collections**: Efficient UI updates with change notifications
- **Event Cleanup**: Proper event subscription management

## Integration Points

### Screenshot Capture Integration
```csharp
// Automatic gallery updates when screenshots are captured
public class ScreenshotCaptureWorkflow
{
    public async Task<CaptureResult> PerformCaptureAsync()
    {
        // 1. Capture screenshot
        var result = await _captureService.CaptureWithAreaSelectionAsync();
        
        // 2. Save to storage
        var uploadResult = await _storageService.UploadFromClipboardAsync(result.ImageData, result.FileName);
        
        // 3. Queue for AI analysis
        await _analysisQueue.QueueAnalysisAsync(screenshotId, uploadResult.BlobName);
        
        // 4. Gallery automatically shows new screenshot via events
        // 5. Gallery automatically updates with AI results when ready
        
        return result;
    }
}
```

### System Tray Integration (Planned)
- **Show/Hide Gallery**: Click tray icon to toggle gallery window
- **Quick Actions**: Right-click menu for capture and gallery operations
- **Notifications**: Toast notifications for completed analysis

## Testing Strategy

```csharp
// Unit tests for view models
[Fact] public async Task LoadScreenshots_ValidData_LoadsSuccessfully()
[Fact] public async Task SearchScreenshots_ValidQuery_ReturnsFilteredResults()
[Fact] public async Task DeleteScreenshot_ValidId_RemovesFromCollection()

// Integration tests with mock services
[Fact] public async Task GalleryWorkflow_CaptureToDisplay_WorksEndToEnd()

// UI tests for WPF components
[Fact] public void ScreenshotTile_DoubleClick_OpensImage()
[Fact] public void SearchBox_TypeText_FiltersResults()
```

## Configuration

### No Configuration Required
The Gallery Viewer component works with the existing storage and analysis services without additional configuration. It automatically:
- Discovers screenshots in the local storage directory
- Subscribes to analysis completion events
- Updates the UI in real-time

### Optional Customization
```csharp
// Customize tile size, layout, or appearance through WPF styles
// Modify search behavior or add additional filters
// Extend context menus or keyboard shortcuts
```