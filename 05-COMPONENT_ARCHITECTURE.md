# Screenshot Manager - Component Architecture

## Overview

This document provides detailed specifications for each component in the Screenshot Manager application. Each component is designed for independent development, testing, and deployment with clear interfaces and minimal coupling.

## Component Map

```
┌─────────────────────────────────────────────────────────────┐
│                    Components Overview                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐      │
│  │   Domain    │    │   Storage   │    │ Clipboard   │      │
│  │  Entities   │    │  Service    │    │ Processing  │      │
│  │             │    │             │    │             │      │
│  └─────────────┘    └─────────────┘    └─────────────┘      │
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐      │
│  │   Web UI    │    │  Realtime   │    │ AI Analysis │      │
│  │ Controller  │    │  Updates    │    │ (Walk+)     │      │
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

## C2: Storage Component

### Responsibility
Handle all Azure Blob Storage operations with optimized clipboard image processing.

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
│   ├── UnitTests/
│   └── IntegrationTests/
└── docs/
    └── storage-design.md
```

### Public Interface
```csharp
public interface IBlobStorageService
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
    
    // Standard operations
    Task<Stream> GetImageStreamAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Uri> GetThumbnailUriAsync(string blobName);
    Task<bool> DeleteImageAsync(string blobName, CancellationToken cancellationToken = default);
    
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
- Azure.Storage.Blobs
- SixLabors.ImageSharp (for image processing)
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options

// Internal dependencies
- Domain component (for BlobReference value object)
```

### Configuration
```csharp
public class StorageOptions
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; } = "screenshots";
    public string ThumbnailContainer { get; set; } = "thumbnails";
    public ImageOptimizationSettings DefaultOptimization { get; set; }
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
    public string BlobName { get; }
}

public enum StorageErrorCode
{
    ConnectionFailure,
    BlobNotFound,
    InsufficientPermissions,
    QuotaExceeded,
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

## C4: Web UI Component

### Responsibility
Handle web interface for clipboard paste operations and gallery display.

### Location
```
Components/WebUI/
├── README.md
├── src/
│   ├── Controllers/
│   ├── Views/
│   ├── ViewModels/
│   ├── JavaScript/
│   └── Styles/
├── tests/
│   ├── ControllerTests/
│   └── JavaScriptTests/
└── docs/
    └── ui-specifications.md
```

### Controller Interface
```csharp
[Route("api/[controller]")]
public class ClipboardController : ControllerBase
{
    [HttpPost("paste")]
    public async Task<ActionResult<PasteResponse>> HandlePasteAsync(
        [FromBody] PasteRequest request,
        CancellationToken cancellationToken)
    {
        // Coordinate clipboard processing and storage
    }
    
    [HttpGet("status/{id}")]
    public async Task<ActionResult<ProcessingStatus>> GetProcessingStatusAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // Return current processing status
    }
}

public class PasteRequest
{
    public string ImageData { get; set; } // Base64 encoded
    public string? SuggestedName { get; set; }
    public DateTime ClientTimestamp { get; set; }
}

public class PasteResponse
{
    public bool Success { get; set; }
    public Guid ScreenshotId { get; set; }
    public string GeneratedFileName { get; set; }
    public string ThumbnailUrl { get; set; }
    public ProcessingStatus Status { get; set; }
}
```

### JavaScript Interface
```javascript
// Main clipboard manager class
class ClipboardManager {
    constructor(options = {}) {
        this.apiEndpoint = options.apiEndpoint || '/api/clipboard';
        this.maxFileSize = options.maxFileSize || 50 * 1024 * 1024;
        this.allowedTypes = options.allowedTypes || ['image/png', 'image/jpeg'];
    }
    
    // Core functionality
    async handlePasteEvent(event) {
        const clipboardData = await this.extractImageFromClipboard(event);
        if (clipboardData) {
            return await this.uploadImage(clipboardData);
        }
    }
    
    async extractImageFromClipboard(event) {
        // Extract image data from clipboard event
    }
    
    async uploadImage(imageData) {
        // Send to backend API
    }
    
    // Event handlers
    setupGlobalPasteHandler() {
        document.addEventListener('paste', this.handlePasteEvent.bind(this));
    }
    
    setupPasteZone(element) {
        element.addEventListener('paste', this.handlePasteEvent.bind(this));
        element.addEventListener('click', () => element.focus());
    }
}
```

### Dependencies
```csharp
// Backend dependencies
- ClipboardProcessing component
- Storage component
- Domain component
- Microsoft.AspNetCore.Mvc
- Microsoft.AspNetCore.SignalR

// Frontend dependencies
- Bootstrap 5.3
- SignalR JavaScript client
- Modern browser with Clipboard API support
```

---

## C5: Realtime Updates Component

### Responsibility
Provide real-time notifications for upload progress and processing status.

### Location
```
Components/RealtimeUpdates/
├── README.md
├── src/
│   ├── Hubs/
│   ├── Services/
│   └── Models/
├── tests/
│   └── RealtimeTests/
└── docs/
    └── signalr-design.md
```

### Public Interface
```csharp
public interface IRealtimeNotificationService
{
    // Connection management
    Task AddToUserGroupAsync(string connectionId, string userId);
    Task RemoveFromUserGroupAsync(string connectionId, string userId);
    
    // Notifications
    Task NotifyUploadStartedAsync(string userId, Guid screenshotId, string fileName);
    Task NotifyUploadProgressAsync(string userId, Guid screenshotId, int progressPercent);
    Task NotifyUploadCompletedAsync(string userId, Guid screenshotId, string thumbnailUrl);
    Task NotifyUploadFailedAsync(string userId, Guid screenshotId, string errorMessage);
    
    // Processing notifications (Walk+)
    Task NotifyProcessingStartedAsync(string userId, Guid screenshotId);
    Task NotifyProcessingCompletedAsync(string userId, Guid screenshotId, ProcessingResult result);
}

// SignalR Hub
public class ScreenshotHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
    }
    
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
    }
    
    private static string GetUserGroupName(string userId) => $"user_{userId}";
}
```

### JavaScript Client Interface
```javascript
class RealtimeClient {
    constructor(hubUrl = '/screenshothub') {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();
        
        this.setupEventHandlers();
    }
    
    async start(userId) {
        await this.connection.start();
        await this.connection.invoke('JoinUserGroup', userId);
    }
    
    setupEventHandlers() {
        this.connection.on('UploadStarted', this.handleUploadStarted.bind(this));
        this.connection.on('UploadProgress', this.handleUploadProgress.bind(this));
        this.connection.on('UploadCompleted', this.handleUploadCompleted.bind(this));
        this.connection.on('UploadFailed', this.handleUploadFailed.bind(this));
    }
    
    // Event handler methods
    handleUploadStarted(screenshotId, fileName) {
        // Update UI to show upload started
    }
    
    handleUploadProgress(screenshotId, progressPercent) {
        // Update progress bar
    }
    
    handleUploadCompleted(screenshotId, thumbnailUrl) {
        // Add to gallery, show success message
    }
}
```

### Dependencies
```csharp
// Backend dependencies
- Microsoft.AspNetCore.SignalR
- Microsoft.Extensions.Logging

// Frontend dependencies
- @microsoft/signalr (JavaScript client)
```

---

## C6: AI Analysis Component (Walk+ Stage)

### Responsibility
Integrate with Azure AI Foundry for OCR and image analysis.

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
    // Text extraction
    Task<TextExtractionResult> ExtractTextAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
    
    // Content analysis
    Task<ContentAnalysisResult> AnalyzeContentAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
    
    // Tag generation
    Task<List<string>> GenerateTagsAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default);
    
    // Health check
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
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
}
```

### Dependencies
```csharp
// External dependencies
- Azure.AI.FormRecognizer
- Azure.AI.Vision.ImageAnalysis
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options

// Internal dependencies
- Domain component
- Storage component (for image retrieval)
```

### Configuration
```csharp
public class AIAnalysisOptions
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string Region { get; set; } = "eastus";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool EnableBatchProcessing { get; set; } = true;
    public int BatchSize { get; set; } = 5;
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
        
        // Storage
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        
        // Clipboard Processing
        services.Configure<ClipboardProcessingOptions>(configuration.GetSection("ClipboardProcessing"));
        services.AddScoped<IClipboardProcessingService, ClipboardProcessingService>();
        
        // Realtime Updates
        services.AddSignalR();
        services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();
        
        // AI Analysis (Walk+)
        services.Configure<AIAnalysisOptions>(configuration.GetSection("AIAnalysis"));
        services.AddScoped<IAIAnalysisService, AIAnalysisService>();
        
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
    public async Task ClipboardToStorage_FullPipeline_WorksCorrectly()
    {
        // Test complete flow from clipboard processing to storage
        var clipboardService = _serviceProvider.GetService<IClipboardProcessingService>();
        var storageService = _serviceProvider.GetService<IBlobStorageService>();
        
        // Execute full pipeline
        var processingResult = await clipboardService.ProcessClipboardDataAsync(testImageData, options);
        var uploadResult = await storageService.UploadFromClipboardAsync(processingResult.ProcessedImageData, processingResult.SuggestedFileName);
        
        // Verify end-to-end functionality
        Assert.True(uploadResult.Success);
    }
}
```

### Component Development Workflow

1. **Develop Domain Component** (No dependencies)
   - Define entities and business rules
   - Create comprehensive unit tests
   - Generate component README

2. **Develop Storage Component** (Depends on Domain)
   - Implement Azure Blob Storage integration
   - Create unit and integration tests
   - Generate component README

3. **Develop Clipboard Processing** (Depends on Domain)
   - Implement image validation and processing
   - Create unit tests with mock images
   - Generate component README

4. **Develop Web UI Component** (Depends on Clipboard + Storage)
   - Implement controllers and views
   - Create JavaScript clipboard handling
   - Test with browser automation tools
   - Generate component README

5. **Develop Realtime Updates** (Depends on Web UI)
   - Implement SignalR hub and notifications
   - Test real-time communication
   - Generate component README

6. **Develop AI Analysis** (Walk+ stage - Depends on Storage)
   - Implement Azure AI integration
   - Create comprehensive testing with sample images
   - Generate component README

---

**Last Updated**: June 17, 2025  
**Version**: 1.0  
**Component Architecture**: Individual component specifications  
**Related Documents**: 04-INTEGRATION_TECHNICAL_DESIGN.md for system integration