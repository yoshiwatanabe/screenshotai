# Screenshot Manager - Integration Technical Design

## Overview

This document focuses on system-level integration, deployment architecture, and cross-component communication patterns. For individual component specifications, see 05-COMPONENT_ARCHITECTURE.md.

## System Integration Architecture

### High-Level Component Flow
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web Browser   │───▶│   ASP.NET Core  │───▶│  Azure Storage  │
│  (Clipboard API)│    │   Web App       │    │   (Blob Store)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌─────────────────┐              │
         └──────────────▶│   SignalR Hub   │◀─────────────┘
                        │ (Real-time)     │
                        └─────────────────┘
                                 │
                        ┌─────────────────┐
                        │  Azure AI       │
                        │  Foundry        │
                        │  (Walk+)        │
                        └─────────────────┘
```

### Data Flow Architecture
```
Clipboard Paste Event
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│ Frontend: Clipboard Detection & Processing                 │
├─────────────────────────────────────────────────────────────┤
│ 1. Detect paste event (Ctrl+V)                            │
│ 2. Extract image from clipboard                           │
│ 3. Convert to base64 for transmission                     │
│ 4. Show immediate preview                                  │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTP POST /api/clipboard/upload
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ Backend: Upload Processing Pipeline                        │
├─────────────────────────────────────────────────────────────┤
│ 1. Validate image data                                     │
│ 2. Generate unique filename                               │
│ 3. Optimize image (compression, resize)                   │
│ 4. Upload to Azure Blob Storage                           │
│ 5. Create thumbnail                                       │
│ 6. Save metadata to database                              │
│ 7. Queue for AI analysis (Walk+)                          │
└─────────────────────┬───────────────────────────────────────┘
                      │ SignalR notification
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ Frontend: Real-time Updates                               │
├─────────────────────────────────────────────────────────────┤
│ 1. Receive upload completion notification                 │
│ 2. Update gallery with new screenshot                     │
│ 3. Show processing status for AI analysis                 │
│ 4. Display final results when ready                       │
└─────────────────────────────────────────────────────────────┘
```

## Technology Stack Integration

### Frontend Technology Stack
```yaml
Core Technologies:
  - HTML5 Clipboard API for paste detection
  - Modern JavaScript (ES2022+) for async operations
  - Bootstrap 5.3 for responsive UI
  - SignalR JavaScript client for real-time updates

Build Process:
  - No bundling required (vanilla JS approach)
  - CSS processed through ASP.NET Core pipeline
  - Static assets served from wwwroot

Browser Support:
  - Chrome 76+ (Clipboard API support)
  - Firefox 90+
  - Safari 13.1+
  - Edge 79+
```

### Backend Technology Stack
```yaml
Core Framework:
  - ASP.NET Core 8.0 (LTS)
  - Entity Framework Core 8.0 (Walk+)
  - SignalR for real-time communication

Azure Integration:
  - Azure.Storage.Blobs SDK
  - Azure.AI.FormRecognizer SDK (Walk+)
  - Application Insights SDK

Performance:
  - Memory caching for frequently accessed data
  - Background services for AI processing
  - Async/await patterns throughout
```

### Database Integration (Walk+ Stage)
```sql
-- Integration considerations for screenshot metadata
CREATE TABLE Screenshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DisplayName NVARCHAR(255),
    BlobName NVARCHAR(255) UNIQUE,
    CreatedAt DATETIME2,
    Source NVARCHAR(50), -- 'Clipboard', 'Upload', etc.
    Status NVARCHAR(50),
    
    -- Indexes for common query patterns
    INDEX IX_Screenshots_CreatedAt_Status (CreatedAt DESC, Status),
    INDEX IX_Screenshots_Source (Source)
);

-- Full-text search integration
CREATE FULLTEXT CATALOG ScreenshotSearch;
CREATE FULLTEXT INDEX ON Screenshots(ExtractedText) 
    KEY INDEX PK_Screenshots;
```

## Deployment Architecture

### Azure Resource Topology
```
┌─────────────────────────────────────────────────────────────┐
│ Resource Group: screenshot-manager-rg                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────────┐  ┌─────────────────┐  ┌───────────────┐ │
│ │ App Service     │  │ Storage Account │  │ Application   │ │
│ │ Plan (Basic B1) │  │ (Standard LRS)  │  │ Insights      │ │
│ └─────────────────┘  └─────────────────┘  └───────────────┘ │
│                                                             │
│ ┌─────────────────┐  ┌─────────────────┐ (Walk+ Stage)     │
│ │ SQL Database    │  │ AI Services     │                   │
│ │ (Basic DTU)     │  │ (East US)       │                   │
│ └─────────────────┘  └─────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
```

### CI/CD Pipeline Integration
```yaml
# GitHub Actions Workflow
name: Deploy Screenshot Manager

on:
  push:
    branches: [main]

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Component Tests
        run: |
          dotnet test Components/*/tests/
          
      - name: Integration Tests  
        run: |
          dotnet test tests/Integration/
          
  deploy:
    needs: integration-tests
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: screenshot-manager-app
```

## Performance and Scalability

### Caching Strategy
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ Browser Cache   │    │ Server Memory   │    │ Azure Storage   │
│                 │    │ Cache           │    │ Cache           │
├─────────────────┤    ├─────────────────┤    ├─────────────────┤
│ • Thumbnails    │    │ • Gallery data  │    │ • CDN (Run)     │
│ • UI assets     │    │ • Search results│    │ • Blob metadata │
│ • User prefs    │    │ • AI results    │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Load Balancing and Scaling
```yaml
# Azure App Service scaling rules
Crawl Stage:
  - Single instance (Always On disabled for cost)
  - Manual scaling as needed

Walk Stage:
  - Auto-scaling based on CPU (2-5 instances)
  - Queue-based scaling for AI processing

Run Stage:
  - Multi-region deployment
  - Azure Front Door for global load balancing
  - Dedicated AI processing tier
```

## Security Integration

### Authentication Flow (Progressive Enhancement)
```
Crawl:  Anonymous access → Session-based storage
         ↓
Walk:   User accounts → Database-backed sessions
         ↓  
Run:    Enterprise SSO → Azure AD B2C integration
```

### Data Protection Strategy
```yaml
Encryption:
  - HTTPS/TLS 1.3 for all communications
  - Azure Storage encryption at rest
  - Application-level sensitive data encryption

Access Control:
  - CORS policies for browser security
  - Rate limiting on upload endpoints
  - Input validation and sanitization

Compliance:
  - GDPR considerations for EU users
  - Data retention policies
  - Audit logging for enterprise features
```

## Monitoring and Observability

### Application Insights Integration
```csharp
// Custom telemetry tracking
services.AddApplicationInsightsTelemetry();

// Custom metrics for clipboard operations
public class ClipboardTelemetry
{
    private readonly TelemetryClient _telemetryClient;
    
    public void TrackClipboardPaste(string source, TimeSpan processingTime)
    {
        _telemetryClient.TrackEvent("ClipboardPaste", 
            properties: new Dictionary<string, string> { ["Source"] = source },
            metrics: new Dictionary<string, double> { ["ProcessingTimeMs"] = processingTime.TotalMilliseconds });
    }
}
```

### Health Checks Integration
```csharp
services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob_storage")
    .AddCheck<DatabaseHealthCheck>("database") // Walk+
    .AddCheck<AIServiceHealthCheck>("ai_service"); // Walk+
```

## Error Handling and Resilience

### Global Error Handling Strategy
```csharp
// Global exception middleware
app.UseExceptionHandler("/Error");

// Specific error handling for component failures
public class ComponentFailureMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (StorageException ex)
        {
            // Handle storage failures gracefully
            await HandleStorageFailure(context, ex);
        }
        catch (AIServiceException ex) // Walk+
        {
            // Degrade gracefully when AI services fail
            await HandleAIServiceFailure(context, ex);
        }
    }
}
```

### Circuit Breaker Pattern
```csharp
// For external service calls
services.AddHttpClient<IAIAnalysisService, AIAnalysisService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

## Configuration Management

### Environment-Specific Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "", // Set via Azure App Settings
    "AzureStorage": ""       // Set via Azure App Settings
  },
  "ComponentSettings": {
    "ClipboardProcessing": {
      "MaxImageSizeMB": 50,
      "OptimizationEnabled": true
    },
    "Storage": {
      "ContainerName": "screenshots",
      "EnableCDN": false  // true in Run stage
    },
    "AIAnalysis": {
      "Enabled": false,   // true in Walk+ stages
      "BatchSize": 10,
      "TimeoutSeconds": 30
    }
  }
}
```

### Feature Flags Integration
```csharp
// Feature flag service for progressive deployment
public class FeatureFlagService
{
    public bool IsAIAnalysisEnabled() => 
        _configuration.GetValue<bool>("Features:AIAnalysis");
        
    public bool IsRealtimeUpdatesEnabled() => 
        _configuration.GetValue<bool>("Features:RealtimeUpdates");
}
```

---

**Last Updated**: June 17, 2025  
**Version**: 1.0  
**Focus**: System integration and deployment architecture  
**Related Documents**: 05-COMPONENT_ARCHITECTURE.md for component-specific details# Screenshot Manager - Technical Design Specifications

## Overview

This document provides detailed technical specifications for implementing the Screenshot Manager application across all three development stages. It includes code structures, API designs, database schemas, and implementation guidelines.

## Technology Stack

### Core Technologies
```yaml
Backend:
  - Framework: ASP.NET Core MVC 8.0
  - Language: C# 12
  - Target Framework: .NET 8.0
  - Architecture: Clean Architecture with Dependency Injection

Frontend:
  - Framework: Razor Pages with Bootstrap 5
  - JavaScript: Vanilla ES6+ (no frameworks for simplicity)
  - CSS Framework: Bootstrap 5.3
  - Icons: Bootstrap Icons

Cloud Services:
  - Hosting: Azure App Service (Linux)
  - Storage: Azure Blob Storage
  - Database: Azure SQL Database (Walk+)
  - AI Services: Azure AI Foundry (Walk+)
  - CDN: Azure CDN (Run)

DevOps:
  - Source Control: GitHub
  - CI/CD: GitHub Actions
  - Monitoring: Application Insights
  - Secrets: Azure Key Vault (Walk+)
```

## Project Structure

```
src/
├── ScreenshotManager.Web/              # Main web application
│   ├── Controllers/
│   ├── Views/
│   ├── ViewModels/
│   ├── wwwroot/
│   └── Program.cs
├── ScreenshotManager.Application/       # Business logic
│   ├── Interfaces/
│   ├── Services/
│   ├── Models/
│   └── DTOs/
├── ScreenshotManager.Domain/           # Domain entities
│   ├── Entities/
│   ├── ValueObjects/
│   └── Enums/
├── ScreenshotManager.Infrastructure/   # External services
│   ├── Storage/
│   ├── AI/
│   ├── Data/
│   └── Configuration/
└── ScreenshotManager.Tests/           # Test projects
    ├── Unit/
    ├── Integration/
    └── E2E/

docs/                                  # Project documentation
├── 00-PROJECT_OVERVIEW.md
├── 01-ROADMAP.md
├── 02-ARCHITECTURE.md
├── 03-FEATURES.md
├── 04-TECHNICAL_DESIGN.md
├── 05-COMPONENT_SPECIFICATIONS.md
└── wireframes/

.github/
└── workflows/
    └── azure-deploy.yml
```

## Stage 1 (Crawl) Implementation

### Domain Models

```csharp
// ScreenshotManager.Domain/Entities/Screenshot.cs
public class Screenshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ThumbnailBlobName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string ContentType { get; set; } = string.Empty;
    public ScreenshotStatus Status { get; set; } = ScreenshotStatus.Uploading;
    
    // Walk+ properties (nullable for now)
    public string? ExtractedText { get; set; }
    public string? AiDescription { get; set; }
    public List<string> Tags { get; set; } = new();
    public ApplicationType? DetectedApplicationType { get; set; }
}

// ScreenshotManager.Domain/Enums/ScreenshotStatus.cs
public enum ScreenshotStatus
{
    Uploading,
    Processing,
    Ready,
    Failed,
    Deleted
}

// ScreenshotManager.Domain/Enums/ApplicationType.cs (Walk+)
public enum ApplicationType
{
    Unknown,
    WebBrowser,
    CodeEditor,
    Terminal,
    OfficeApplication,
    DesignTool,
    MessagingApp,
    OperatingSystem
}
```

### Application Services

```csharp
// ScreenshotManager.Application/Interfaces/IBlobStorageService.cs
public interface IBlobStorageService
{
    Task<UploadResult> UploadScreenshotAsync(
        IFormFile file, 
        CancellationToken cancellationToken = default);
    
    Task<Stream> GetScreenshotStreamAsync(
        string blobName, 
        CancellationToken cancellationToken = default);
    
    Task<Uri> GetThumbnailUriAsync(string thumbnailBlobName);
    
    Task<bool> DeleteScreenshotAsync(
        string blobName, 
        CancellationToken cancellationToken = default);
    
    Task<byte[]> GenerateThumbnailAsync(
        Stream originalImageStream, 
        int maxWidth = 300, 
        int maxHeight = 200);
}

// ScreenshotManager.Application/Models/UploadResult.cs
public class UploadResult
{
    public bool Success { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ThumbnailBlobName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
```

### Infrastructure Implementation

```csharp
// ScreenshotManager.Infrastructure/Storage/AzureBlobStorageService.cs
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly AzureStorageOptions _options;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureBlobStorageService> logger,
        IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<UploadResult> UploadScreenshotAsync(
        IFormFile file, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validation
            if (!IsValidImageFile(file))
            {
                return new UploadResult 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid file type" 
                };
            }

            // Generate unique blob names
            var fileExtension = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid()}{fileExtension}";
            var thumbnailBlobName = $"thumbnails/{Guid.NewGuid()}_thumb.jpg";

            // Upload original file
            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using var fileStream = file.OpenReadStream();
            
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            var metadata = new Dictionary<string, string>
            {
                ["OriginalFileName"] = file.FileName,
                ["UploadTime"] = DateTime.UtcNow.ToString("O"),
                ["ContentType"] = file.ContentType
            };

            await blobClient.UploadAsync(
                fileStream, 
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders,
                    Metadata = metadata
                }, 
                cancellationToken);

            // Generate and upload thumbnail
            fileStream.Position = 0;
            var thumbnailBytes = await GenerateThumbnailAsync(fileStream);
            
            var thumbnailClient = containerClient.GetBlobClient(thumbnailBlobName);
            await thumbnailClient.UploadAsync(
                new MemoryStream(thumbnailBytes),
                new BlobHttpHeaders { ContentType = "image/jpeg" },
                cancellationToken: cancellationToken);

            return new UploadResult
            {
                Success = true,
                BlobName = blobName,
                ThumbnailBlobName = thumbnailBlobName,
                FileSizeBytes = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload screenshot {FileName}", file.FileName);
            return new UploadResult 
            { 
                Success = false, 
                ErrorMessage = "Upload failed" 
            };
        }
    }

    private static bool IsValidImageFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        var allowedContentTypes = new[] 
        { 
            "image/png", "image/jpeg", "image/gif", "image/webp" 
        };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension) && 
               allowedContentTypes.Contains(file.ContentType);
    }

    public async Task<byte[]> GenerateThumbnailAsync(
        Stream originalImageStream, 
        int maxWidth = 300, 
        int maxHeight = 200)
    {
        // Implementation using System.Drawing or ImageSharp
        // For now, simplified implementation
        using var image = await Image.LoadAsync(originalImageStream);
        
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxWidth, maxHeight),
            Mode = ResizeMode.Max
        }));

        using var thumbnailStream = new MemoryStream();
        await image.SaveAsJpegAsync(thumbnailStream);
        return thumbnailStream.ToArray();
    }
}
```

### Configuration

```csharp
// ScreenshotManager.Infrastructure/Configuration/AzureStorageOptions.cs
public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "screenshots";
    public string ThumbnailContainer { get; set; } = "thumbnails";
    public int MaxFileSizeMB { get; set; } = 25;
    public int MaxBatchSizeMB { get; set; } = 100;
    public int MaxFilesPerBatch { get; set; } = 10;
}

// ScreenshotManager.Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenshotManagerServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<AzureStorageOptions>(
            configuration.GetSection(AzureStorageOptions.SectionName));

        // Azure Blob Storage
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureStorageOptions>>();
            return new BlobServiceClient(options.Value.ConnectionString);
        });

        // Application Services
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IScreenshotService, ScreenshotService>();

        return services;
    }
}
```

### Web Layer

```csharp
// ScreenshotManager.Web/Controllers/UploadController.cs
[Route("api/[controller]")]
[ApiController]
public class UploadController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IScreenshotService _screenshotService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IBlobStorageService blobStorageService,
        IScreenshotService screenshotService,
        ILogger<UploadController> logger)
    {
        _blobStorageService = blobStorageService;
        _screenshotService = screenshotService;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(100_000_000)] // 100MB
    public async Task<IActionResult> UploadScreenshots(
        List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var results = new List<UploadResultDto>();

        foreach (var file in files)
        {
            try
            {
                var uploadResult = await _blobStorageService.UploadScreenshotAsync(
                    file, cancellationToken);

                if (uploadResult.Success)
                {
                    var screenshot = new Screenshot
                    {
                        OriginalFileName = file.FileName,
                        BlobName = uploadResult.BlobName,
                        ThumbnailBlobName = uploadResult.ThumbnailBlobName,
                        FileSizeBytes = uploadResult.FileSizeBytes,
                        ContentType = uploadResult.ContentType,
                        Status = ScreenshotStatus.Ready
                    };

                    await _screenshotService.SaveScreenshotAsync(screenshot, cancellationToken);

                    results.Add(new UploadResultDto
                    {
                        FileName = file.FileName,
                        Success = true,
                        ScreenshotId = screenshot.Id
                    });
                }
                else
                {
                    results.Add(new UploadResultDto
                    {
                        FileName = file.FileName,
                        Success = false,
                        ErrorMessage = uploadResult.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process upload for {FileName}", file.FileName);
                results.Add(new UploadResultDto
                {
                    FileName = file.FileName,
                    Success = false,
                    ErrorMessage = "Processing failed"
                });
            }
        }

        return Ok(results);
    }
}

// ScreenshotManager.Web/ViewModels/GalleryViewModel.cs
public class GalleryViewModel
{
    public List<ScreenshotItemViewModel> Screenshots { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public class ScreenshotItemViewModel
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string FullImageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string FormattedFileSize => FormatFileSize(FileSizeBytes);
    public List<string> Tags { get; set; } = new();

    private static string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            max /= scale;
        }
        return "0 Bytes";
    }
}
```

## Stage 2 (Walk) Extensions

### AI Analysis Service

```csharp
// ScreenshotManager.Application/Interfaces/IImageAnalysisService.cs
public interface IImageAnalysisService
{
    Task<TextExtractionResult> ExtractTextAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default);
    
    Task<List<string>> GenerateTagsAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default);
    
    Task<string> GenerateDescriptionAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationType> DetectApplicationTypeAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default);
}

// ScreenshotManager.Application/Models/TextExtractionResult.cs
public class TextExtractionResult
{
    public string ExtractedText { get; set; } = string.Empty;
    public double OverallConfidence { get; set; }
    public List<TextRegion> TextRegions { get; set; } = new();
    public string DetectedLanguage { get; set; } = "en";
}

public class TextRegion
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

### Database Schema (Walk+)

```sql
-- Screenshots table
CREATE TABLE Screenshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OriginalFileName NVARCHAR(255) NOT NULL,
    BlobName NVARCHAR(255) NOT NULL UNIQUE,
    ThumbnailBlobName NVARCHAR(255) NOT NULL,
    FileSizeBytes BIGINT NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status INT NOT NULL DEFAULT 0,
    
    -- AI Analysis fields (Walk+)
    ExtractedText NVARCHAR(MAX) NULL,
    AiDescription NVARCHAR(1000) NULL,
    DetectedApplicationType INT NULL,
    ProcessedAt DATETIME2 NULL,
    
    INDEX IX_Screenshots_UploadedAt (UploadedAt DESC),
    INDEX IX_Screenshots_Status (Status),
    FULLTEXT INDEX FT_Screenshots_ExtractedText (ExtractedText)
);

-- Tags table
CREATE TABLE Tags (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Category NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_Tags_Name (Name),
    INDEX IX_Tags_Category (Category)
);

-- Screenshot Tags junction table
CREATE TABLE ScreenshotTags (
    ScreenshotId UNIQUEIDENTIFIER NOT NULL,
    TagId INT NOT NULL,
    Confidence DECIMAL(5,4) NULL,
    IsUserGenerated BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    PRIMARY KEY (ScreenshotId, TagId),
    FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id) ON DELETE CASCADE,
    FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
);
```

## Frontend Implementation

### Upload Interface (JavaScript)

```javascript
// wwwroot/js/upload.js
class ScreenshotUploader {
    constructor() {
        this.maxFileSize = 25 * 1024 * 1024; // 25MB
        this.maxBatchSize = 100 * 1024 * 1024; // 100MB
        this.allowedTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
        this.uploadQueue = [];
        this.initializeEventListeners();
    }

    initializeEventListeners() {
        const dropZone = document.getElementById('upload-drop-zone');
        const fileInput = document.getElementById('file-input');

        // Drag and drop handlers
        dropZone.addEventListener('dragover', this.handleDragOver.bind(this));
        dropZone.addEventListener('drop', this.handleDrop.bind(this));
        
        // File input handler
        fileInput.addEventListener('change', this.handleFileSelect.bind(this));
        
        // Upload button
        document.getElementById('upload-btn').addEventListener('click', this.startUpload.bind(this));
    }

    handleDragOver(event) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
        document.getElementById('upload-drop-zone').classList.add('drag-over');
    }

    handleDrop(event) {
        event.preventDefault();
        document.getElementById('upload-drop-zone').classList.remove('drag-over');
        
        const files = Array.from(event.dataTransfer.files);
        this.processFiles(files);
    }

    handleFileSelect(event) {
        const files = Array.from(event.target.files);
        this.processFiles(files);
    }

    processFiles(files) {
        const validFiles = files.filter(file => this.validateFile(file));
        
        if (validFiles.length > 0) {
            this.addToQueue(validFiles);
            this.updateUploadPreview();
        }
    }

    validateFile(file) {
        if (!this.allowedTypes.includes(file.type)) {
            this.showError(`Invalid file type: ${file.name}`);
            return false;
        }
        
        if (file.size > this.maxFileSize) {
            this.showError(`File too large: ${file.name} (${this.formatFileSize(file.size)})`);
            return false;
        }
        
        return true;
    }

    addToQueue(files) {
        const totalSize = this.uploadQueue.reduce((sum, f) => sum + f.size, 0) + 
                         files.reduce((sum, f) => sum + f.size, 0);
        
        if (totalSize > this.maxBatchSize) {
            this.showError('Batch size too large. Please upload fewer files.');
            return;
        }
        
        files.forEach(file => {
            const uploadItem = {
                id: this.generateId(),
                file: file,
                status: 'queued',
                progress: 0,
                error: null
            };
            this.uploadQueue.push(uploadItem);
        });
    }

    async startUpload() {
        const queuedItems = this.uploadQueue.filter(item => item.status === 'queued');
        
        if (queuedItems.length === 0) {
            this.showError('No files to upload');
            return;
        }

        // Upload files in parallel (max 3 concurrent)
        const concurrency = 3;
        const chunks = this.chunkArray(queuedItems, concurrency);
        
        for (const chunk of chunks) {
            await Promise.all(chunk.map(item => this.uploadFile(item)));
        }
    }

    async uploadFile(uploadItem) {
        try {
            uploadItem.status = 'uploading';
            this.updateItemDisplay(uploadItem);

            const formData = new FormData();
            formData.append('files', uploadItem.file);

            const response = await fetch('/api/upload', {
                method: 'POST',
                body: formData,
                onUploadProgress: (event) => {
                    if (event.lengthComputable) {
                        uploadItem.progress = (event.loaded / event.total) * 100;
                        this.updateItemDisplay(uploadItem);
                    }
                }
            });

            if (response.ok) {
                const result = await response.json();
                uploadItem.status = 'completed';
                uploadItem.result = result[0]; // First result for this file
            } else {
                uploadItem.status = 'failed';
                uploadItem.error = 'Upload failed';
            }
        } catch (error) {
            uploadItem.status = 'failed';
            uploadItem.error = error.message;
        }
        
        this.updateItemDisplay(uploadItem);
    }

    updateUploadPreview() {
        const container = document.getElementById('upload-preview');
        container.innerHTML = '';
        
        this.uploadQueue.forEach(item => {
            const itemElement = this.createUploadItemElement(item);
            container.appendChild(itemElement);
        });
    }

    createUploadItemElement(uploadItem) {
        const div = document.createElement('div');
        div.className = 'upload-item';
        div.id = `upload-item-${uploadItem.id}`;
        
        div.innerHTML = `
            <div class="upload-item-info">
                <span class="filename">${uploadItem.file.name}</span>
                <span class="filesize">${this.formatFileSize(uploadItem.file.size)}</span>
            </div>
            <div class="upload-progress">
                <div class="progress">
                    <div class="progress-bar" style="width: ${uploadItem.progress}%"></div>
                </div>
                <span class="status">${uploadItem.status}</span>
            </div>
            <div class="upload-actions">
                <button class="btn-remove" onclick="uploader.removeFromQueue('${uploadItem.id}')">×</button>
            </div>
        `;
        
        return div;
    }

    formatFileSize(bytes) {
        const units = ['B', 'KB', 'MB', 'GB'];
        let size = bytes;
        let unitIndex = 0;
        
        while (size >= 1024 && unitIndex < units.length - 1) {
            size /= 1024;
            unitIndex++;
        }
        
        return `${size.toFixed(1)} ${units[unitIndex]}`;
    }

    generateId() {
        return Math.random().toString(36).substr(2, 9);
    }
}

// Initialize uploader when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.uploader = new ScreenshotUploader();
});
```

### Gallery Interface (Razor View)

```html
@* Views/Gallery/Index.cshtml *@
@model GalleryViewModel

<div class="gallery-container">
    <div class="gallery-header">
        <h2>Screenshot Gallery</h2>
        <div class="gallery-actions">
            <a href="/upload" class="btn btn-primary">
                <i class="bi bi-upload"></i> Upload Screenshots
            </a>
        </div>
    </div>

    @if (Model.Screenshots.Any())
    {
        <div class="gallery-grid">
            @foreach (var screenshot in Model.Screenshots)
            {
                <div class="screenshot-card" data-id="@screenshot.Id">
                    <div class="screenshot-thumbnail">
                        <img src="@screenshot.ThumbnailUrl" 
                             alt="@screenshot.OriginalFileName"
                             loading="lazy"
                             onclick="openScreenshotModal('@screenshot.Id')" />
                        <div class="screenshot-overlay">
                            <button class="btn btn-sm btn-light" 
                                    onclick="downloadScreenshot('@screenshot.Id')">
                                <i class="bi bi-download"></i>
                            </button>
                            <button class="btn btn-sm btn-light" 
                                    onclick="shareScreenshot('@screenshot.Id')">
                                <i class="bi bi-share"></i>
                            </button>
                        </div>
                    </div>
                    <div class="screenshot-info">
                        <h6 class="screenshot-title">@screenshot.OriginalFileName</h6>
                        <small class="text-muted">
                            @screenshot.UploadedAt.ToString("MMM dd, yyyy")
                            • @screenshot.FormattedFileSize
                        </small>
                        @if (screenshot.Tags.Any())
                        {
                            <div class="screenshot-tags">
                                @foreach (var tag in screenshot.Tags.Take(3))
                                {
                                    <span class="badge bg-secondary">@tag</span>
                                }
                                @if (screenshot.Tags.Count > 3)
                                {
                                    <span class="badge bg-light text-dark">+@(screenshot.Tags.Count - 3)</span>
                                }
                            </div>
                        }
                    </div>
                </div>
            }
        </div>

        <!-- Pagination -->
        @if (Model.TotalPages > 1)
        {
            <nav aria-label="Gallery pagination">
                <ul class="pagination justify-content-center">
                    @if (Model.HasPreviousPage)
                    {
                        <li class="page-item">
                            <a class="page-link" href="?page=@(Model.CurrentPage - 1)">Previous</a>
                        </li>
                    }
                    
                    @for (int i = Math.Max(1, Model.CurrentPage - 2); 
                          i <= Math.Min(Model.TotalPages, Model.CurrentPage + 2); 
                          i++)
                    {
                        <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                            <a class="page-link" href="?page=@i">@i</a>
                        </li>
                    }
                    
                    @if (Model.HasNextPage)
                    {
                        <li class="page-item">
                            <a class="page-link" href="?page=@(Model.CurrentPage + 1)">Next</a>
                        </li>
                    }
                </ul>
            </nav>
        }
    }
    else
    {
        <div class="empty-state">
            <div class="empty-state-icon">
                <i class="bi bi-images"></i>
            </div>
            <h4>No screenshots yet</h4>
            <p>Upload your first screenshot to get started.</p>
            <a href="/upload" class="btn btn-primary">Upload Now</a>
        </div>
    }
</div>

<!-- Screenshot Modal -->
<div class="modal fade" id="screenshotModal" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="screenshotModalTitle">Screenshot Details</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body" id="screenshotModalBody">
                <!-- Content loaded dynamically -->
            </div>
        </div>
    </div>
</div>
```

## Configuration and Deployment

### Application Settings

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureStorage": {
    "ConnectionString": "", // Set via environment variables
    "ContainerName": "screenshots",
    "ThumbnailContainer": "thumbnails",
    "MaxFileSizeMB": 25,
    "MaxBatchSizeMB": 100,
    "MaxFilesPerBatch": 10
  },
  "AzureAI": {
    "Endpoint": "", // Set via environment variables
    "ApiKey": "", // Set via environment variables
    "Region": "eastus"
  },
  "ApplicationInsights": {
    "ConnectionString": "" // Set via environment variables
  }
}

// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### GitHub Actions Workflow

```yaml
# .github/workflows/azure-deploy.yml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  AZURE_WEBAPP_NAME: screenshot-manager-app
  AZURE_WEBAPP_PACKAGE_PATH: './src/ScreenshotManager.Web'
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore
      working-directory: ./src
      
    - name: Build application
      run: dotnet build --configuration Release --no-restore
      working-directory: ./src
      
    - name: Run tests
      run: dotnet test --configuration Release --no-build --verbosity normal
      working-directory: ./src
      
    - name: Publish application
      run: dotnet publish --configuration Release --no-build --output ./publish
      working-directory: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
      
    - name: Deploy to Azure App Service
      if: github.ref == 'refs/heads/main'
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}/publish
```

## Testing Strategy

### Unit Test Example

```csharp
// ScreenshotManager.Tests/Unit/BlobStorageServiceTests.cs
public class BlobStorageServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<ILogger<AzureBlobStorageService>> _mockLogger;
    private readonly IOptions<AzureStorageOptions> _options;
    private readonly AzureBlobStorageService _service;

    public BlobStorageServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockLogger = new Mock<ILogger<AzureBlobStorageService>>();
        _options = Options.Create(new AzureStorageOptions
        {
            ContainerName = "test-screenshots",
            MaxFileSizeMB = 25
        });
        
        _service = new AzureBlobStorageService(
            _mockBlobServiceClient.Object,
            _mockLogger.Object,
            _options);
    }

    [Fact]
    public async Task UploadScreenshotAsync_ValidFile_ReturnsSuccessResult()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.png", "image/png", 1024);
        var mockContainerClient = new Mock<BlobContainerClient>();
        var mockBlobClient = new Mock<BlobClient>();
        
        _mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(mockContainerClient.Object);
            
        mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        var result = await _service.UploadScreenshotAsync(mockFile);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.BlobName);
        Assert.NotEmpty(result.ThumbnailBlobName);
    }

    private static Mock<IFormFile> CreateMockFormFile(string fileName, string contentType, long length)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return mockFile;
    }
}
```

### Integration Test Example

```csharp
// ScreenshotManager.Tests/Integration/UploadControllerTests.cs
public class UploadControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UploadControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UploadScreenshots_ValidImage_ReturnsSuccess()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageBytes = CreateTestImageBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(imageContent, "files", "test.png");

        // Act
        var response = await _client.PostAsync("/api/upload", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<UploadResultDto>>(responseString);
        
        Assert.Single(results);
        Assert.True(results[0].Success);
    }

    private static byte[] CreateTestImageBytes()
    {
        // Create a simple 1x1 PNG for testing
        return new byte[] { /* PNG header and minimal image data */ };
    }
}
```

## Performance Considerations

### Caching Strategy

```csharp
// ScreenshotManager.Application/Services/CachedScreenshotService.cs
public class CachedScreenshotService : IScreenshotService
{
    private readonly IScreenshotService _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

    public async Task<PagedResult<Screenshot>> GetScreenshotsAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var cacheKey = $"screenshots_page_{page}_size_{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<Screenshot> cachedResult))
        {
            return cachedResult;
        }

        var result = await _inner.GetScreenshotsAsync(page, pageSize, cancellationToken);
        
        _cache.Set(cacheKey, result, _cacheDuration);
        
        return result;
    }
}
```

### Database Optimization

```sql
-- Performance indexes for Walk+ stage
CREATE INDEX IX_Screenshots_ExtractedText_FT 
ON Screenshots (ExtractedText) 
WHERE ExtractedText IS NOT NULL;

CREATE INDEX IX_ScreenshotTags_TagId_ScreenshotId 
ON ScreenshotTags (TagId, ScreenshotId) 
INCLUDE (Confidence, IsUserGenerated);

-- Partitioning strategy for large datasets (Run stage)
CREATE PARTITION FUNCTION ScreenshotDatePartition (DATETIME2)
AS RANGE RIGHT FOR VALUES 
('2025-01-01', '2025-02-01', '2025-03-01', '2025-04-01');
```

---

## Security Implementation

### Input Validation

```csharp
// ScreenshotManager.Application/Validators/UploadRequestValidator.cs
public class UploadRequestValidator : AbstractValidator<IFormFile>
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
    private static readonly string[] AllowedMimeTypes = 
    { 
        "image/png", "image/jpeg", "image/gif", "image/webp" 
    };

    public UploadRequestValidator()
    {
        RuleFor(x => x.Length)
            .LessThanOrEqualTo(26214400) // 25MB
            .WithMessage("File size must not exceed 25MB");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .Must(HaveValidExtension)
            .WithMessage("Invalid file extension");

        RuleFor(x => x.ContentType)
            .Must(BeValidMimeType)
            .WithMessage("Invalid file type");
    }

    private static bool HaveValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }

    private static bool BeValidMimeType(string contentType)
    {
        return AllowedMimeTypes.Contains(contentType);
    }
}
```

---

**Last Updated**: June 17, 2025  
**Version**: 1.0  
**Technical Lead**: Development Team  
**Next Review**: After Stage 1 implementation completion