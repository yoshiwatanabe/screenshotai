# Screenshot Manager - Component Architecture

## Overview

This document provides detailed specifications for each component in the Screenshot Manager application. Each component is designed for independent development, testing, and deployment with clear interfaces and minimal coupling.

## Component Map

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       System Tray Desktop Application                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Domain    │  │    Local    │  │ Screenshot  │  │   Gallery   │    │
│  │  Entities   │  │   Storage   │  │   Capture   │  │   Viewer    │    │
│  │      ✅     │  │      ✅     │  │      ✅     │  │      ✅     │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Azure AI  │  │   System    │  │   Global    │  │   Toast     │    │
│  │   Vision    │  │    Tray     │  │   Hotkey    │  │Notifications│    │
│  │      ✅     │  │  (Pending)  │  │      ✅     │  │  (Pending)  │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## C1: Domain Component

### Responsibility
Core business entities and value objects representing the screenshot domain model.

### Location
```
Components/Domain/
├── README.md
├── src/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Enums/
│   └── Interfaces/
├── tests/
│   └── DomainTests/
└── docs/
    └── domain-model.md
```

### Public Interface
```csharp
// Core Entity
public class Screenshot
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public string BlobName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ScreenshotSource Source { get; private set; }
    public ScreenshotStatus Status { get; private set; }
    
    // Factory methods
    public static Screenshot CreateFromClipboard(string displayName, string blobName);
    public static Screenshot CreateFromUpload(string displayName, string blobName);
    
    // Business methods
    public void UpdateDisplayName(string newName);
    public void MarkAsProcessed();
    public void MarkAsFailed(string reason);
    
    // Walk+ extensions
    public void AddAIAnalysis(string extractedText, List<string> tags);
}

// Value Objects
public class BlobReference
{
    public string ContainerName { get; }
    public string BlobName { get; }
    public Uri FullUri { get; }
}

// Enums
public enum ScreenshotSource { Clipboard, FileUpload, DragDrop, API }
public enum ScreenshotStatus { Processing, Ready, Failed, Deleted }
```

### Dependencies
- None (pure domain component)

### Testing Strategy
```csharp
public class ScreenshotTests
{
    [Fact]
    public void CreateFromClipboard_ValidData_CreatesScreenshot()
    {
        // Test entity creation and business rules
    }
    
    [Fact]
    public void UpdateDisplayName_ValidName_UpdatesSuccessfully()
    {
        // Test business logic validation
    }
}
```

### Component README Template
```markdown
# Domain Component

## Responsibility
- Define core business entities
- Enforce business rules and invariants
- Provide factory methods for entity creation

## Public Interface
- Screenshot entity with business methods
- Value objects for complex types
- Domain enums and constants

## Dependencies
- None (pure domain layer)

## Usage Example
```csharp
var screenshot = Screenshot.CreateFromClipboard("Screenshot_001", "blob123");
screenshot.MarkAsProcessed();
```

## Testing
- Unit tests for all business rules
- Property-based testing for invariants
- No external dependencies to mock
```

---

## C2: Local Storage Component ✅ COMPLETE

### Responsibility
Handle all local file system storage operations with optimized clipboard image processing and privacy protection.

### Location
```
Components/Storage/
├── README.md
├── src/
│   ├── Services/
│   ├── Models/
│   ├── Configuration/
│   └── Extensions/
├── tests/
│   └── LocalStorageTests/
└── docs/
    └── storage-design.md
```

### Public Interface
```csharp
public interface ILocalStorageService
{
    // Primary clipboard operations
    Task<UploadResult> UploadFromClipboardAsync(
        byte[] imageData, 
        string fileName,
        CancellationToken cancellationToken = default);
    
    // Image optimization
    Task<OptimizedImageResult> OptimizeImageAsync(
        byte[] originalImage,
        ImageOptimizationSettings settings);
    
    // Local file operations
    Task<Stream> GetImageStreamAsync(string fileName, CancellationToken cancellationToken = default);
    Task<string> GetThumbnailPathAsync(string fileName);
    Task<string> GetImagePathAsync(string fileName);
    Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default);
    
    // Health check
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

// Result types
public class UploadResult
{
    public bool Success { get; set; }
    public string BlobName { get; set; }
    public string ThumbnailBlobName { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OptimizedImageResult
{
    public byte[] OptimizedImageData { get; set; }
    public byte[] ThumbnailData { get; set; }
    public long OriginalSize { get; set; }
    public long OptimizedSize { get; set; }
    public double CompressionRatio { get; set; }
}
```

### Dependencies
```csharp
// External dependencies
- SixLabors.ImageSharp (for image processing)
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options
- Microsoft.Extensions.Configuration

// Internal dependencies
- Domain component (for BlobReference value object)

// Privacy Benefits
- No cloud dependencies
- No network requirements
- Complete local operation
```

### Configuration
```csharp
public class StorageOptions
{
    public string ScreenshotsDirectory { get; set; } = "~/Documents/Screenshots";
    public string ThumbnailsDirectory { get; set; } = "~/Documents/Screenshots/Thumbnails";
    public ImageOptimizationSettings DefaultOptimization { get; set; }
    public bool CreateDirectoriesIfNotExist { get; set; } = true;
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
}

public class ImageOptimizationSettings
{
    public int MaxWidth { get; set; } = 1920;
    public int MaxHeight { get; set; } = 1080;
    public int JpegQuality { get; set; } = 85;
    public bool GenerateThumbnail { get; set; } = true;
    public Size ThumbnailSize { get; set; } = new(300, 200);
}
```

### Error Handling
```csharp
public class StorageException : Exception
{
    public StorageErrorCode ErrorCode { get; }
    public string FileName { get; }
}

public enum StorageErrorCode
{
    DirectoryAccessFailure,
    FileNotFound,
    InsufficientPermissions,
    DiskSpaceExceeded,
    InvalidImageFormat
}
```

---

## C3: Screenshot Capture Component ✅ COMPLETE

### Responsibility
Handle global hotkey registration, area selection overlay, and screenshot capture functionality.

### Location
```
Components/ScreenshotCapture/
├── README.md
├── src/
│   ├── Services/
│   ├── Models/
│   ├── UI/
│   └── Extensions/
├── tests/
│   └── CaptureTests/
└── docs/
    └── capture-workflow.md
```

### Public Interface
```csharp
public interface IGlobalHotkeyService
{
    bool RegisterCaptureHotkey(Action onCaptureRequested);
    void UnregisterCaptureHotkey();
    string GetHotkeyDisplayString();
    bool IsActive { get; }
}

public interface IAreaSelectionService
{
    Task<Rectangle> ShowAreaSelectionAsync(CancellationToken cancellationToken = default);
    Rectangle GetVirtualScreenBounds();
    bool IsSelectionActive { get; }
}

public interface IScreenshotCaptureService
{
    Task<CaptureResult> CaptureAreaAsync(Rectangle area, CancellationToken cancellationToken = default);
    Task<CaptureResult> CaptureWithAreaSelectionAsync(CancellationToken cancellationToken = default);
    Task<CaptureResult> PerformCompleteCaptureWorkflowAsync(CancellationToken cancellationToken = default);
    string GenerateScreenshotFilename(DateTime? timestamp = null);
}

public class CaptureResult
{
    public bool Success { get; set; }
    public byte[]? ImageData { get; set; }
    public string? FileName { get; set; }
    public Rectangle CapturedArea { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

### Dependencies
```csharp
// External dependencies
- System.Windows.Forms (for global hotkey and overlay)
- System.Drawing.Common (for screen capture)
- Microsoft.Extensions.Logging

// Internal dependencies
- Domain component (for entities)
- Storage component (for local file operations)
```

### Key Features
- **Global Hotkey**: Ctrl+Print Screen with Win+Shift+C fallback
- **Area Selection**: Interactive overlay with real-time feedback
- **Screen Capture**: Multi-monitor support with efficient capture
- **Complete Workflow**: Hotkey → Selection → Capture → Save

---

## C4: Gallery Viewer Component ✅ COMPLETE

### Responsibility
Provide a WPF-based gallery interface for viewing, managing, and searching screenshots with real-time AI analysis updates.

### Location
```
Components/GalleryViewer/
├── README.md
├── src/
│   ├── Views/
│   ├── ViewModels/
│   ├── Models/
│   ├── Services/
│   └── Extensions/
├── tests/
│   └── GalleryTests/
└── docs/
    └── gallery-specifications.md
```

### WPF Gallery Interface
```csharp
public class GalleryViewModel : ObservableObject
{
    public ObservableCollection<ScreenshotViewModel> Screenshots { get; }
    public ObservableCollection<ScreenshotViewModel> FilteredScreenshots { get; }
    public string SearchText { get; set; }
    public ScreenshotViewModel? SelectedScreenshot { get; set; }
    public GalleryStatistics? Statistics { get; set; }
    
    // Commands
    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
}

public class ScreenshotViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string ThumbnailPath { get; set; }
    public string? AiDescription { get; set; }
    public bool HasAiAnalysis { get; set; }
    public bool IsAnalyzing { get; set; }
    public List<string> Tags { get; set; }
    public string? ExtractedText { get; set; }
    
    // Commands
    public ICommand OpenImageCommand { get; }
    public ICommand OpenInExplorerCommand { get; }
    public ICommand DeleteCommand { get; }
}
```

### Gallery Services
```csharp
public interface IGalleryService
{
    Task<List<ScreenshotViewModel>> LoadScreenshotsAsync(CancellationToken cancellationToken = default);
    Task<List<ScreenshotViewModel>> RefreshGalleryAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteScreenshotAsync(Guid screenshotId, CancellationToken cancellationToken = default);
    Task<List<ScreenshotViewModel>> SearchScreenshotsAsync(string searchText, CancellationToken cancellationToken = default);
    Task<GalleryStatistics> GetGalleryStatisticsAsync();
    
    // Real-time update events
    event EventHandler<ScreenshotAddedEventArgs>? ScreenshotAdded;
    event EventHandler<ScreenshotAnalysisUpdatedEventArgs>? AnalysisUpdated;
}
```

### Dependencies
```csharp
// Component dependencies
- Storage component (local file operations)
- AzureVisionAnalysis component (AI results and events)
- Domain component (entities)

// WPF framework dependencies
- CommunityToolkit.Mvvm (MVVM framework)
- Microsoft.Xaml.Behaviors.Wpf (UI behaviors)

// Key Features
- Grid-based tile layout with thumbnails
- Real-time search across multiple fields
- Status indicators and progress visualization
- Keyboard shortcuts (F5, Delete, Enter, ESC)
- Event-driven updates from capture and analysis
```

---

## C5: Azure Vision Analysis Component ✅ COMPLETE

### Responsibility
Provide Azure AI Vision integration for screenshot content analysis with background processing queue and comprehensive description generation.

### Location
```
Components/AzureVisionAnalysis/
├── README.md
├── src/
│   ├── Services/
│   ├── Models/
│   ├── Configuration/
│   └── Extensions/
├── tests/
│   └── AnalysisTests/
└── docs/
    └── ai-integration.md
```

### Public Interface
```csharp
public interface IAzureVisionService
{
    Task<VisionAnalysisResult> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<VisionAnalysisResult> AnalyzeImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<(string caption, double confidence)> GetImageCaptionAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
    Task<bool> CanAnalyzeImageAsync(string imagePath);
}

public interface IAnalysisQueueService
{
    Task<AnalysisJob> QueueAnalysisAsync(Guid screenshotId, string imagePath, CancellationToken cancellationToken = default);
    Task<AnalysisQueueStatus> GetQueueStatusAsync();
    Task<VisionAnalysisResult?> GetAnalysisResultAsync(Guid screenshotId);
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task StopProcessingAsync();
    
    // Real-time analysis completion events
    event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;
}

// Analysis models
public class VisionAnalysisResult
{
    public bool Success { get; set; }
    public string? MainCaption { get; set; }
    public double MainCaptionConfidence { get; set; }
    public List<string> DenseCaptions { get; set; }
    public List<DetectedObject> Objects { get; set; }
    public List<string> Tags { get; set; }
    public string? ExtractedText { get; set; }
    public string GetComprehensiveDescription();
}

public class AnalysisJob
{
    public Guid Id { get; set; }
    public Guid ScreenshotId { get; set; }
    public string ImagePath { get; set; }
    public DateTime QueuedAt { get; set; }
    public AnalysisJobStatus Status { get; set; }
}
```

### Azure Configuration
```csharp
public class AzureVisionOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? ModelDeploymentName { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool IncludeDenseCaptions { get; set; } = true;
    public bool IncludeObjects { get; set; } = true;
    public bool IncludeText { get; set; } = true;
    public bool IncludeTags { get; set; } = true;
}
```

### Dependencies
```csharp
// Azure dependencies
- Azure.AI.Vision.ImageAnalysis
- Azure.Core

// Background processing
- System.Threading.Channels
- Microsoft.Extensions.Hosting.Abstractions

// Component dependencies
- Domain component
- Storage component

// Key Features
- Background processing queue using channels
- Comprehensive retry logic with exponential backoff
- Event-driven real-time result notifications
- Support for Azure AI Foundry endpoints
- Configurable analysis features (captions, objects, OCR, tags)
```

---

## C6: System Tray Application (Integration Layer) 🚧 PENDING

### Responsibility
Main application host that integrates all components into a system tray desktop application with global hotkey support.

### Location
```
ScreenshotManagerApp/
├── Program.cs
├── App.xaml
├── App.xaml.cs
├── Services/
│   ├── TrayIconService.cs
│   ├── NotificationService.cs
│   └── AppLifecycleService.cs
├── Configuration/
│   ├── AppSettings.cs
│   └── DependencyInjection.cs
├── Resources/
│   ├── app-icon.ico
│   └── tray-icons/
└── appsettings.json
```

### Main Application Structure
```csharp
public class ScreenshotManagerApp : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITrayIconService _trayIconService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IAnalysisQueueService _analysisQueue;
    private readonly INotificationService _notificationService;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Configure dependency injection
        // 2. Initialize system tray icon
        // 3. Register global hotkey (Ctrl+Print Screen)
        // 4. Start background services
        // 5. Hide main window (run in tray)
    }
    
    private async void OnScreenshotRequested()
    {
        var captureService = _serviceProvider.GetRequiredService<IScreenshotCaptureService>();
        var result = await captureService.PerformCompleteCaptureWorkflowAsync();
        
        if (result.Success)
        {
            _notificationService.ShowSuccess($"📸 Screenshot saved: {result.FileName}");
            
            // Queue for AI analysis
            var analysisQueue = _serviceProvider.GetRequiredService<IAnalysisQueueService>();
            await analysisQueue.QueueAnalysisAsync(Guid.NewGuid(), result.FileName!);
        }
        else
        {
            _notificationService.ShowError($"❌ Capture failed: {result.ErrorMessage}");
        }
    }
}

public interface ITrayIconService
{
    void Initialize();
    void ShowContextMenu();
    void UpdateIcon(string iconPath);
    void ShowNotification(string title, string message, NotificationType type);
    void Dispose();
}

public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);
    void ShowAnalysisComplete(string description);
}
```

### System Tray Context Menu
```csharp
// Context Menu Structure:
"📸 Capture Screenshot (Ctrl+PrtScn)" -> Trigger screenshot capture
"🖼️ Show Gallery"                     -> Open gallery window
"📊 View Statistics"                   -> Show analysis statistics
"⚙️ Settings"                          -> Open settings dialog
"❓ Help"                              -> Show help/documentation
"❌ Exit"                              -> Exit application

// Toast Notifications:
"📸 Screenshot captured and saved"
"🤖 AI analysis completed: [description]"
"❌ Screenshot capture cancelled"
"⚠️ AI analysis failed - check connection"
```

### Dependency Injection Configuration
```csharp
public static class DependencyInjection
{
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // All existing component services
        services.AddDomainServices();
        services.AddLocalStorageServices(configuration);
        services.AddScreenshotCaptureServices();
        services.AddAzureVisionAnalysisServices(configuration);
        services.AddGalleryViewerServices();
        
        // System tray application services
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<INotificationService, WindowsNotificationService>();
        services.AddSingleton<IAppLifecycleService, AppLifecycleService>();
        
        // Configuration
        services.Configure<AppSettings>(configuration.GetSection("App"));
        
        return services;
    }
}
```

### Application Configuration
```csharp
public class AppSettings
{
    public bool StartMinimized { get; set; } = true;
    public bool ShowToastNotifications { get; set; } = true;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool HotkeyEnabled { get; set; } = true;
    public string HotkeyDisplayText { get; set; } = "Ctrl+Print Screen";
    public bool GalleryAutoRefresh { get; set; } = true;
    public int NotificationTimeoutSeconds { get; set; } = 5;
}
```

### Dependencies
```csharp
// System integration
- System.Windows.Forms (for NotifyIcon and system tray)
- Microsoft.Win32 (for Windows registry and auto-start)
- Microsoft.Toolkit.Win32.UI.Controls (for toast notifications)

// Application framework
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration

// All component dependencies
- Domain, Storage, ScreenshotCapture, AzureVisionAnalysis, GalleryViewer

// Key Features
- System tray with context menu
- Global hotkey registration and handling
- Toast notifications for user feedback
- Background service orchestration
- Complete component integration
- Application lifecycle management
```

---

## Component Integration Guidelines

### Dependency Injection Setup
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenshotManagerComponents(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Domain (no registration needed - pure entities)
        
        // Local Storage
        services.AddLocalStorageServices(configuration);
        
        // Clipboard Processing
        services.Configure<ClipboardProcessingOptions>(configuration.GetSection("ClipboardProcessing"));
        services.AddScoped<IClipboardProcessingService, ClipboardProcessingService>();
        
        // Local Search & Metadata
        services.Configure<SearchOptions>(configuration.GetSection("Search"));
        services.AddScoped<ISearchService, LocalSearchService>();
        
        // Desktop UI Services
        services.AddSingleton<ClipboardMonitorService>();
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<HotkeyService>();
        
        // AI Analysis (Walk+ - optional)
        services.Configure<AIAnalysisOptions>(configuration.GetSection("AIAnalysis"));
        services.AddScoped<IAIAnalysisService, LocalAIAnalysisService>();
        
        return services;
    }
}
```

### Component Testing Strategy
```csharp
// Component isolation testing
public class ComponentIntegrationTests
{
    [Fact]
    public async Task ClipboardToLocalStorage_FullPipeline_WorksCorrectly()
    {
        // Test complete flow from clipboard processing to local storage
        var clipboardService = _serviceProvider.GetService<IClipboardProcessingService>();
        var storageService = _serviceProvider.GetService<ILocalStorageService>();
        
        // Execute full pipeline
        var processingResult = await clipboardService.ProcessClipboardDataAsync(testImageData, options);
        var uploadResult = await storageService.UploadFromClipboardAsync(processingResult.ProcessedImageData, processingResult.SuggestedFileName);
        
        // Verify end-to-end functionality
        Assert.True(uploadResult.Success);
        Assert.True(File.Exists(await storageService.GetImagePathAsync(uploadResult.BlobName)));
    }
    
    [Fact]
    public async Task LocalSearch_IndexAndSearch_WorksCorrectly()
    {
        // Test search functionality with local database
        var searchService = _serviceProvider.GetService<ISearchService>();
        var screenshot = CreateTestScreenshot();
        
        // Index screenshot
        await searchService.IndexScreenshotAsync(screenshot);
        
        // Search for it
        var results = await searchService.SearchByTextAsync("test screenshot");
        
        // Verify search works
        Assert.Contains(screenshot.Id, results.Select(r => r.Id));
    }
}
```

### Component Development Workflow

1. **Develop Domain Component** ✅ COMPLETE (No dependencies)
   - Define entities and business rules
   - Create comprehensive unit tests
   - Generate component README

2. **Develop Local Storage Component** ✅ COMPLETE (Depends on Domain)
   - Implement local file system storage
   - Create unit and integration tests
   - Generate component README

3. **Develop Screenshot Capture Component** ✅ COMPLETE (Depends on Domain + Storage)
   - Implement global hotkey and area selection
   - Create complete capture workflow
   - Test with various screen configurations
   - Generate component README

4. **Develop Azure Vision Analysis Component** ✅ COMPLETE (Depends on Domain + Storage)
   - Implement Azure AI Vision integration
   - Create background processing queue
   - Test with sample images and error scenarios
   - Generate component README

5. **Develop Gallery Viewer Component** ✅ COMPLETE (Depends on Domain + Storage + AzureVision)
   - Implement WPF gallery interface with MVVM
   - Create real-time update handling
   - Test search and management features
   - Generate component README

6. **Develop System Tray Application** 🚧 PENDING (Depends on All Components)
   - Implement main application host
   - Create system tray integration
   - Add toast notifications and lifecycle management
   - Test complete end-to-end workflow

---

**Last Updated**: June 29, 2025  
**Version**: 2.0  
**Component Architecture**: Privacy-first local desktop application  
**Related Documents**: 04-INTEGRATION_TECHNICAL_DESIGN.md for system integration

## Architecture Changes Summary

### Version 2.0 Changes (Privacy-First Local Storage)
- **Storage**: Changed from Azure Blob Storage to local file system
- **UI**: Changed from Web UI to Desktop Application (WPF/MAUI)
- **Search**: Changed from cloud-based to local SQLite with FTS5
- **AI**: Changed from cloud-only to privacy-first local models with optional cloud
- **Realtime**: Replaced SignalR with local desktop notifications
- **Privacy**: Complete local operation with no mandatory network dependencies

### Benefits of Local-First Architecture
- ✅ **Privacy Protection**: Screenshots never leave user's machine
- ✅ **Offline Operation**: Works completely without internet
- ✅ **Performance**: Direct file access, no network latency
- ✅ **Cost**: No cloud storage or processing fees
- ✅ **Control**: User owns and controls all data
- ✅ **Security**: No cloud attack surface