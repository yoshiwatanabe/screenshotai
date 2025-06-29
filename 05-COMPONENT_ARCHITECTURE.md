# Screenshot Manager - Component Architecture

## Overview

This document provides detailed specifications for each component in the Screenshot Manager application. Each component is designed for independent development, testing, and deployment with clear interfaces and minimal coupling.

## Component Map

```
┌─────────────────────────────────────────────────────────────┐
│             Desktop Components Overview                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐      │
│  │   Domain    │    │    Local    │    │ Clipboard   │      │
│  │  Entities   │    │   Storage   │    │ Processing  │      │
│  │             │    │             │    │             │      │
│  └─────────────┘    └─────────────┘    └─────────────┘      │
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐      │
│  │ Desktop UI  │    │   Local AI  │    │   Search    │      │
│  │ (WPF/MAUI)  │    │ (Optional)  │    │ & Metadata  │      │
│  │             │    │             │    │             │      │
│  └─────────────┘    └─────────────┘    └─────────────┘      │
└─────────────────────────────────────────────────────────────┘
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

## C3: Clipboard Processing Component

### Responsibility
Handle clipboard image detection, validation, and preprocessing for upload.

### Location
```
Components/ClipboardProcessing/
├── README.md
├── src/
│   ├── Services/
│   ├── Validators/
│   ├── Models/
│   └── Extensions/
├── tests/
│   └── ClipboardTests/
└── docs/
    └── clipboard-processing.md
```

### Public Interface
```csharp
public interface IClipboardProcessingService
{
    // Core processing
    Task<ClipboardProcessingResult> ProcessClipboardDataAsync(
        byte[] clipboardData,
        ClipboardProcessingOptions options,
        CancellationToken cancellationToken = default);
    
    // Filename generation
    Task<string> GenerateSmartFilenameAsync(
        byte[] imageData,
        DateTime timestamp,
        CancellationToken cancellationToken = default);
    
    // Validation
    Task<ValidationResult> ValidateClipboardImageAsync(byte[] imageData);
    
    // Metadata extraction
    Task<ImageMetadata> ExtractMetadataAsync(byte[] imageData);
}

public class ClipboardProcessingResult
{
    public bool Success { get; set; }
    public byte[] ProcessedImageData { get; set; }
    public string SuggestedFileName { get; set; }
    public ImageMetadata Metadata { get; set; }
    public List<string> ValidationWarnings { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ImageMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; }
    public long FileSizeBytes { get; set; }
    public bool HasTransparency { get; set; }
    public ColorProfile ColorProfile { get; set; }
}
```

### Dependencies
```csharp
// External dependencies
- SixLabors.ImageSharp
- Microsoft.Extensions.Logging

// Internal dependencies
- Domain component (for enums and value objects)
```

### Business Rules
```csharp
public class ClipboardImageValidator
{
    public ValidationResult Validate(byte[] imageData)
    {
        var result = new ValidationResult();
        
        // Size validation (configurable limits)
        if (imageData.Length > _options.MaxFileSizeBytes)
            result.AddError("Image exceeds maximum size limit");
        
        // Format validation
        if (!IsValidImageFormat(imageData))
            result.AddError("Unsupported image format");
        
        // Dimension validation
        var metadata = ExtractImageMetadata(imageData);
        if (metadata.Width > _options.MaxDimensions.Width || 
            metadata.Height > _options.MaxDimensions.Height)
            result.AddWarning("Image will be resized to fit limits");
        
        return result;
    }
}
```

---

## C4: Desktop UI Component

### Responsibility
Handle desktop interface for clipboard capture, gallery display, and screenshot management.

### Location
```
Components/DesktopUI/
├── README.md
├── src/
│   ├── Views/
│   ├── ViewModels/
│   ├── Controls/
│   ├── Services/
│   └── Styles/
├── tests/
│   └── UITests/
└── docs/
    └── ui-specifications.md
```

### Desktop Interface
```csharp
public class MainWindowViewModel : INotifyPropertyChanged
{
    // Clipboard monitoring
    public async Task StartClipboardMonitoringAsync();
    public async Task HandleClipboardImageAsync(byte[] imageData);
    
    // Gallery management
    public ObservableCollection<ScreenshotViewModel> Screenshots { get; }
    public async Task LoadScreenshotsAsync();
    public async Task DeleteScreenshotAsync(Guid id);
    
    // Search and filter
    public string SearchText { get; set; }
    public async Task SearchScreenshotsAsync(string query);
}

public class ScreenshotViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string ThumbnailPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public ScreenshotSource Source { get; set; }
    public ScreenshotStatus Status { get; set; }
    
    // Commands
    public ICommand OpenCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }
}
```

### Desktop Controls
```csharp
// Clipboard monitoring service
public class ClipboardMonitorService
{
    public event EventHandler<ClipboardImageEventArgs> ImageCopied;
    
    public void StartMonitoring()
    {
        // Start monitoring clipboard for image changes
    }
    
    public void StopMonitoring()
    {
        // Stop clipboard monitoring
    }
}

// System tray integration
public class TrayIconService
{
    public void ShowNotification(string title, string message);
    public void UpdateIcon(string iconPath);
    public void ShowContextMenu();
}

// Global hotkey service
public class HotkeyService
{
    public void RegisterHotkey(Keys key, ModifierKeys modifiers, Action callback);
    public void UnregisterHotkey(Keys key, ModifierKeys modifiers);
}
```

### Dependencies
```csharp
// Component dependencies
- ClipboardProcessing component
- Local Storage component
- Domain component

// Desktop framework dependencies
- WPF or .NET MAUI
- System.Windows.Forms (for system tray)
- Microsoft.Win32 (for Windows registry/system integration)

// Cross-platform considerations
- Avalonia UI (alternative to WPF for cross-platform)
- Platform-specific clipboard APIs
```

---

## C5: Local Search & Metadata Component

### Responsibility
Provide fast local search capabilities and metadata management for screenshots.

### Location
```
Components/SearchMetadata/
├── README.md
├── src/
│   ├── Services/
│   ├── Models/
│   ├── Database/
│   └── Indexing/
├── tests/
│   └── SearchTests/
└── docs/
    └── search-design.md
```

### Public Interface
```csharp
public interface ISearchService
{
    // Search operations
    Task<SearchResult> SearchAsync(SearchQuery query);
    Task<List<Screenshot>> SearchByTextAsync(string text);
    Task<List<Screenshot>> SearchByTagsAsync(IEnumerable<string> tags);
    Task<List<Screenshot>> SearchByDateRangeAsync(DateTime start, DateTime end);
    
    // Indexing operations
    Task IndexScreenshotAsync(Screenshot screenshot);
    Task UpdateScreenshotIndexAsync(Screenshot screenshot);
    Task RemoveFromIndexAsync(Guid screenshotId);
    
    // Metadata management
    Task<ScreenshotMetadata> GetMetadataAsync(Guid screenshotId);
    Task SaveMetadataAsync(ScreenshotMetadata metadata);
}

// Search models
public class SearchQuery
{
    public string Text { get; set; }
    public IEnumerable<string> Tags { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ScreenshotSource? Source { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}

public class SearchResult
{
    public List<Screenshot> Screenshots { get; set; }
    public int TotalCount { get; set; }
    public TimeSpan SearchTime { get; set; }
}
```

### Local Database Schema
```sql
-- SQLite database for local metadata storage
CREATE TABLE Screenshots (
    Id TEXT PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    FileName TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    Source INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    ExtractedText TEXT,
    Tags TEXT, -- JSON array
    FailureReason TEXT
);

CREATE TABLE ScreenshotTags (
    ScreenshotId TEXT NOT NULL,
    Tag TEXT NOT NULL,
    Confidence REAL,
    PRIMARY KEY (ScreenshotId, Tag),
    FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id)
);

-- Full-text search index
CREATE VIRTUAL TABLE ScreenshotSearch USING fts5(
    Id, DisplayName, ExtractedText, Tags,
    content='Screenshots'
);
```

### Dependencies
```csharp
// Database dependencies
- Microsoft.Data.Sqlite
- Microsoft.EntityFrameworkCore.Sqlite

// Search dependencies
- SQLite FTS5 extension
- Microsoft.Extensions.Logging

// Component dependencies
- Domain component
- Local Storage component
```

---

## C6: Local AI Analysis Component (Walk+ Stage)

### Responsibility
Provide privacy-first AI analysis with local models and optional cloud services.

### Location
```
Components/AIAnalysis/
├── README.md
├── src/
│   ├── Services/
│   ├── Models/
│   ├── Configuration/
│   └── Extensions/
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docs/
    └── ai-integration.md
```

### Public Interface
```csharp
public interface IAIAnalysisService
{
    // Local AI analysis
    Task<TextExtractionResult> ExtractTextLocallyAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
    
    // Optional cloud analysis (with user consent)
    Task<TextExtractionResult> ExtractTextCloudAsync(
        Stream imageStream,
        AIProvider provider,
        CancellationToken cancellationToken = default);
    
    // Content analysis
    Task<ContentAnalysisResult> AnalyzeContentAsync(
        Stream imageStream,
        bool useLocalModel = true,
        CancellationToken cancellationToken = default);
    
    // Tag generation
    Task<List<string>> GenerateTagsAsync(
        Stream imageStream,
        bool useLocalModel = true,
        CancellationToken cancellationToken = default);
    
    // Model management
    Task<bool> IsLocalModelAvailableAsync();
    Task DownloadLocalModelAsync(IProgress<double> progress);
}

public class TextExtractionResult
{
    public string ExtractedText { get; set; }
    public double OverallConfidence { get; set; }
    public List<TextRegion> TextRegions { get; set; }
    public string DetectedLanguage { get; set; }
}

public class ContentAnalysisResult
{
    public ApplicationType DetectedApplicationType { get; set; }
    public List<string> DetectedUIElements { get; set; }
    public string Description { get; set; }
    public double AnalysisConfidence { get; set; }
    public bool ProcessedLocally { get; set; }
    public AIProvider? UsedProvider { get; set; }
}

public enum AIProvider
{
    Local,
    OpenAI,
    AzureAI,
    Ollama
}
```

### Dependencies
```csharp
// Local AI dependencies
- ML.NET or similar for local models
- Tesseract OCR for local text extraction
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options

// Optional cloud dependencies (user configurable)
- OpenAI API client
- Azure.AI.Vision.ImageAnalysis
- HTTP clients for various AI services

// Internal dependencies
- Domain component
- Local Storage component
```

### Configuration
```csharp
public class AIAnalysisOptions
{
    public bool PreferLocalModels { get; set; } = true;
    public string LocalModelPath { get; set; } = "./Models";
    public bool AllowCloudServices { get; set; } = false;
    
    // Cloud service configuration (optional)
    public string? OpenAIApiKey { get; set; }
    public string? AzureEndpoint { get; set; }
    public string? AzureApiKey { get; set; }
    
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
}
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

3. **Develop Clipboard Processing** (Depends on Domain)
   - Implement image validation and processing
   - Create unit tests with mock images
   - Generate component README

4. **Develop Desktop UI Component** (Depends on Clipboard + Storage)
   - Implement WPF/MAUI views and view models
   - Create desktop clipboard handling
   - Test with UI automation tools
   - Generate component README

5. **Develop Local Search & Metadata** (Depends on Domain + Storage)
   - Implement SQLite database and search
   - Test full-text search functionality
   - Generate component README

6. **Develop Local AI Analysis** (Walk+ stage - Depends on Storage)
   - Implement local AI model integration
   - Create comprehensive testing with sample images
   - Generate component README

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