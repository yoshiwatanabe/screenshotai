# ScreenshotCapture Component

## Responsibility

Handles global hotkey registration, area selection overlay, and screenshot capture functionality for the Screenshot Manager application.

## Key Features

- **Global Hotkey Support**: Registers `Ctrl+Print Screen` with automatic fallback to `Win+Shift+C`
- **Area Selection Overlay**: Interactive screen overlay for selecting capture areas
- **Multi-Screen Support**: Works across multiple monitors with virtual screen bounds
- **Real-time Capture**: Fast screen area capture with immediate feedback
- **Error Handling**: Comprehensive error handling and user cancellation support

## Public Interface

### Core Services

```csharp
// Global hotkey registration
services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();

// Area selection functionality  
services.AddScoped<IAreaSelectionService, AreaSelectionService>();

// Complete screenshot capture workflow
services.AddScoped<IScreenshotCaptureService, ScreenshotCaptureService>();
```

### Key Methods

```csharp
// Register global hotkey
bool RegisterCaptureHotkey(Action onCaptureRequested);

// Show area selection overlay
Task<Rectangle> ShowAreaSelectionAsync(CancellationToken cancellationToken = default);

// Capture selected area and save to storage
Task<CaptureResult> PerformCompleteCaptureWorkflowAsync(CancellationToken cancellationToken = default);
```

## Dependencies

### External Dependencies
- `Microsoft.Extensions.Logging.Abstractions` - Logging support
- `Microsoft.Extensions.Options` - Configuration options
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI support
- `System.Drawing.Common` - Graphics and bitmap operations
- `System.Windows.Forms` - Windows Forms for overlay and hotkey handling

### Internal Dependencies
- `Domain` component - Core entities and models
- `Storage` component - Local file storage operations

## Usage Example

```csharp
// Register services
services.AddScreenshotCaptureServices();
services.AddLocalStorageServices(configuration);

// Use in application
public class ScreenshotApp
{
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IScreenshotCaptureService _captureService;
    
    public ScreenshotApp(IGlobalHotkeyService hotkeyService, IScreenshotCaptureService captureService)
    {
        _hotkeyService = hotkeyService;
        _captureService = captureService;
    }
    
    public void Start()
    {
        // Register global hotkey
        _hotkeyService.RegisterCaptureHotkey(async () =>
        {
            var result = await _captureService.PerformCompleteCaptureWorkflowAsync();
            if (result.Success)
            {
                Console.WriteLine($"Screenshot saved: {result.FileName}");
            }
        });
    }
}
```

## User Workflow

1. **Hotkey Press**: User presses `Ctrl+Print Screen`
2. **Area Selection**: Overlay appears with crosshair cursor
3. **Selection**: User drags to select area (with real-time size display)
4. **Capture**: Screenshot is captured and immediately saved to local storage
5. **Result**: Success/failure feedback provided

## Technical Details

### Hotkey Registration
- **Primary**: `Ctrl+Print Screen` (leverages existing screenshot key association)
- **Fallback**: `Win+Shift+C` (if primary hotkey registration fails)
- **Cross-Process**: Uses Win32 API for system-wide hotkey registration

### Area Selection Overlay
- **Full Screen**: Covers all monitors with virtual screen bounds
- **Semi-Transparent**: 30% opacity black overlay for visibility
- **Interactive**: Real-time selection rectangle with size display
- **Cancellation**: ESC key cancels selection
- **Minimum Size**: Prevents accidental tiny selections (10x10 minimum)

### Screen Capture
- **High Performance**: Direct GDI+ screen capture
- **Multi-Format**: Saves as PNG for quality and transparency support
- **Error Resilient**: Handles capture failures gracefully
- **Fast Processing**: Optimized for responsive user experience

## Error Handling

```csharp
// Hotkey registration failures
public class HotkeyRegistrationException : Exception

// User cancellation
public class AreaSelectionCancelledException : OperationCanceledException

// Capture result with error information
public class CaptureResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    // ... other properties
}
```

## Performance Characteristics

- **Hotkey Response**: < 100ms from press to overlay display
- **Screen Capture**: < 500ms for typical screen areas
- **Memory Efficient**: Properly disposes graphics resources
- **Non-Blocking**: Async operations don't freeze UI

## Testing

Create comprehensive tests covering:
- Hotkey registration and unregistration
- Area selection with various scenarios (cancel, small areas, etc.)
- Screen capture across different resolutions
- Error handling and edge cases
- Memory management and resource disposal

## Security Considerations

- **System-Wide Access**: Hotkey registration requires appropriate permissions
- **Screen Content**: Captures whatever is visible on screen
- **Local Storage**: All data stays on user's machine
- **No Network**: No external dependencies or data transmission