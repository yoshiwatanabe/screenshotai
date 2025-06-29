# AzureVisionAnalysis Component

## Responsibility

Provides Azure AI Vision integration for screenshot content analysis with background processing queue and comprehensive description generation.

## Key Features

- **Azure AI Vision Integration**: Full integration with Azure AI Vision API from Azure AI Foundry
- **Background Processing**: Asynchronous analysis queue that doesn't block screenshot capture
- **Comprehensive Analysis**: Main captions, dense captions, object detection, OCR, and tags
- **Retry Logic**: Automatic retry with exponential backoff for failed requests
- **Event-Driven**: Real-time notifications when analysis completes
- **Configuration Validation**: Ensures proper Azure credentials and settings

## Public Interface

### Core Services

```csharp
// Azure Vision API integration
services.AddSingleton<IAzureVisionService, AzureVisionService>();

// Background processing queue
services.AddSingleton<IAnalysisQueueService, AnalysisQueueService>();

// Complete service registration
services.AddAzureVisionAnalysisServices(configuration);
```

### Key Methods

```csharp
// Queue image for background analysis
Task<AnalysisJob> QueueAnalysisAsync(Guid screenshotId, string imagePath);

// Get comprehensive analysis results
Task<VisionAnalysisResult> AnalyzeImageAsync(string imagePath);

// Get analysis results when ready
Task<VisionAnalysisResult?> GetAnalysisResultAsync(Guid screenshotId);

// Monitor queue status
Task<AnalysisQueueStatus> GetQueueStatusAsync();
```

## Configuration

### appsettings.json
```json
{
  "AzureVision": {
    "Endpoint": "https://your-vision-service.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key-here",
    "ModelDeploymentName": "gpt-4-vision",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000,
    "MaxFileSizeBytes": 20971520,
    "IncludeDenseCaptions": true,
    "IncludeObjects": true,
    "IncludeText": true,
    "IncludeTags": true
  }
}
```

### User Secrets (Recommended)
```bash
dotnet user-secrets set "AzureVision:ApiKey" "your-actual-api-key"
dotnet user-secrets set "AzureVision:Endpoint" "https://your-endpoint.cognitiveservices.azure.com/"
```

## Dependencies

### External Dependencies
- `Azure.AI.Vision.ImageAnalysis` - Azure Vision API client
- `Microsoft.Extensions.Logging.Abstractions` - Logging support
- `Microsoft.Extensions.Options` - Configuration options
- `Microsoft.Extensions.Hosting.Abstractions` - Background service hosting
- `System.Threading.Channels` - High-performance async queuing

### Internal Dependencies
- `Domain` component - Core entities and models
- `Storage` component - Local file access for image analysis

## Usage Example

```csharp
// Configure services
services.AddAzureVisionAnalysisServices(configuration);
services.AddLocalStorageServices(configuration);

// Use in application
public class ScreenshotAnalysisApp
{
    private readonly IAnalysisQueueService _analysisQueue;
    private readonly IAzureVisionService _visionService;
    
    public ScreenshotAnalysisApp(IAnalysisQueueService analysisQueue, IAzureVisionService visionService)
    {
        _analysisQueue = analysisQueue;
        _visionService = visionService;
        
        // Subscribe to analysis completion events
        _analysisQueue.AnalysisCompleted += OnAnalysisCompleted;
    }
    
    public async Task ProcessScreenshotAsync(Guid screenshotId, string imagePath)
    {
        // Queue for background analysis
        var job = await _analysisQueue.QueueAnalysisAsync(screenshotId, imagePath);
        Console.WriteLine($"Queued analysis job: {job.Id}");
    }
    
    private void OnAnalysisCompleted(object? sender, AnalysisCompletedEventArgs e)
    {
        if (e.Success)
        {
            Console.WriteLine($"Analysis completed for {e.ScreenshotId}: {e.Result.MainCaption}");
        }
        else
        {
            Console.WriteLine($"Analysis failed for {e.ScreenshotId}");
        }
    }
}
```

## Complete Workflow Integration

```csharp
// 1. Screenshot captured and saved locally
var captureResult = await screenshotService.PerformCompleteCaptureWorkflowAsync();

// 2. Queue for background AI analysis
if (captureResult.Success)
{
    await analysisQueue.QueueAnalysisAsync(screenshotId, captureResult.FileName);
}

// 3. Analysis happens in background
// 4. Results available via GetAnalysisResultAsync() or AnalysisCompleted event
```

## Analysis Results

### VisionAnalysisResult Structure
```csharp
public class VisionAnalysisResult
{
    public string? MainCaption { get; set; }                    // "A person using a computer"
    public double MainCaptionConfidence { get; set; }           // 0.95
    public List<string> DenseCaptions { get; set; }             // ["A laptop on a desk", "Code on a screen"]
    public List<DetectedObject> Objects { get; set; }           // [Monitor, Keyboard, Mouse]
    public List<string> Tags { get; set; }                      // ["technology", "computer", "programming"]
    public string? ExtractedText { get; set; }                  // OCR text from image
    public string GetComprehensiveDescription() { get; set; }   // Combined description
}
```

## Background Processing

### Queue Architecture
- **Unbounded Channel**: High-performance async queue using `System.Threading.Channels`
- **Single Reader**: One background service processes jobs sequentially
- **Multiple Writers**: Multiple screenshot captures can queue simultaneously
- **Event-Driven**: Real-time notifications when analysis completes

### Processing Flow
1. Screenshot captured → `QueueAnalysisAsync()` called
2. Job added to channel queue
3. Background service picks up job
4. Azure Vision API called with retry logic
5. Results stored and `AnalysisCompleted` event fired
6. Gallery can refresh to show AI descriptions

## Error Handling

### Retry Logic
- **Automatic Retries**: Up to 3 attempts with exponential backoff
- **Transient Failures**: Network issues, rate limiting handled gracefully
- **Permanent Failures**: Invalid images, API errors reported immediately

### Configuration Validation
```csharp
// Validates endpoint URL, API key, and other settings
options.Validate(); // Throws InvalidOperationException if invalid

// Service availability check
bool isAvailable = await visionService.IsServiceAvailableAsync();
```

## Performance Characteristics

- **Non-Blocking**: Screenshot capture never waits for AI analysis
- **Parallel Processing**: Multiple images can be analyzed simultaneously
- **Memory Efficient**: Streams image data, doesn't hold in memory
- **Fast Queuing**: < 10ms to queue analysis job
- **Analysis Time**: 2-10 seconds per image depending on content complexity

## Security Considerations

- **API Key Protection**: Use user secrets or secure configuration
- **Local Processing**: Images analyzed via API, but stored locally
- **No Data Retention**: Azure Vision doesn't store your images
- **Secure Transmission**: HTTPS communication with Azure
- **Configuration Validation**: Prevents misconfigured deployments

## Testing Strategy

```csharp
// Unit tests for Azure Vision service
[Fact] public async Task AnalyzeImage_ValidImage_ReturnsResults()
[Fact] public async Task AnalyzeImage_InvalidImage_ReturnsFailure()
[Fact] public async Task QueueAnalysis_ValidJob_CompletesSuccessfully()

// Integration tests with test images
[Fact] public async Task EndToEnd_CaptureAndAnalyze_WorksCorrectly()
```

## Monitoring and Observability

- **Structured Logging**: Comprehensive logging with correlation IDs
- **Queue Metrics**: Track queue depth, processing times, success rates
- **Error Tracking**: Failed analysis jobs with detailed error information
- **Performance Metrics**: Analysis times, API response times, retry counts