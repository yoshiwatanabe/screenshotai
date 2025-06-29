# Screenshot Manager - System Architecture

## Overview

This document outlines the complete system architecture for the Screenshot Manager application - a privacy-first, local-only screenshot management tool with AI-powered content analysis.

## Architecture Principles

### Privacy-First Design
- **Complete Local Operation**: All screenshots stored on user's machine
- **Optional Cloud AI**: User controls when AI analysis occurs via API
- **No Mandatory Network**: Core functionality works offline
- **Zero Data Retention**: Cloud services don't store user images

### Component-Based Architecture
- **Modular Design**: Independent, testable components
- **Clean Interfaces**: Well-defined service contracts
- **Dependency Injection**: Proper IoC container usage
- **Event-Driven**: Real-time updates via events

### Desktop Application Model
- **System Tray Application**: Runs persistently in background
- **Global Hotkey**: System-wide screenshot capture (Ctrl+Print Screen)
- **WPF Gallery**: Rich desktop interface for management
- **Background Processing**: Non-blocking AI analysis

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           System Tray Application                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │
│  │   Global Hotkey │  │   Gallery       │  │   System Tray          │  │
│  │   Monitoring    │  │   Window        │  │   Management            │  │
│  │                 │  │                 │  │                         │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
┌───────────────────▼───┐  ┌────────▼────────┐  ┌──▼─────────────────────┐
│  Screenshot Capture   │  │ Gallery Viewer  │  │  Azure Vision Analysis │
│                       │  │                 │  │                        │
│  • Global Hotkey      │  │ • WPF Interface │  │ • Background Queue     │
│  • Area Selection     │  │ • Real-time     │  │ • Azure AI Vision API  │
│  • Screen Capture     │  │   Updates       │  │ • Event-driven Results │
│                       │  │ • Search        │  │                        │
└───────────┬───────────┘  └─────────────────┘  └────────────────────────┘
            │                        │                          │
            │                        │                          │
┌───────────▼────────────────────────▼──────────────────────────▼────────┐
│                          Storage & Domain Layer                        │
│                                                                         │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────┐ │
│  │     Domain      │    │  Local Storage  │    │   Metadata DB       │ │
│  │                 │    │                 │    │   (SQLite)          │ │
│  │ • Screenshot    │    │ • File System   │    │                     │ │
│  │   Entity        │    │ • Image         │    │ • AI Results        │ │
│  │ • Business      │    │   Optimization  │    │ • Search Index      │ │
│  │   Rules         │    │ • Thumbnails    │    │ • Metadata          │ │
│  │                 │    │                 │    │                     │ │
│  └─────────────────┘    └─────────────────┘    └─────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

## Component Architecture

### Core Components

#### 1. System Tray Application (Main Host)
```csharp
public class ScreenshotManagerApp : Application
{
    // Responsibilities:
    // - Host all components in single process
    // - Manage system tray icon and context menu
    // - Handle application lifecycle
    // - Coordinate component interactions
    // - Manage dependency injection container
}
```

#### 2. Screenshot Capture Component ✅ IMPLEMENTED
```csharp
// Services:
IGlobalHotkeyService     // Global hotkey registration (Ctrl+Print Screen)
IAreaSelectionService    // Interactive area selection overlay
IScreenshotCaptureService // Complete capture workflow

// Workflow:
Hotkey Press → Area Selection → Screen Capture → Local Save
```

#### 3. Local Storage Component ✅ IMPLEMENTED
```csharp
// Services:
ILocalStorageService     // File system operations
                        // Image optimization and thumbnails
                        // Privacy-first local storage

// Features:
Local File System Storage → Image Optimization → Thumbnail Generation
```

#### 4. Azure Vision Analysis Component ✅ IMPLEMENTED
```csharp
// Services:
IAzureVisionService      // Azure AI Vision API integration
IAnalysisQueueService    // Background processing queue

// Workflow:
Background Queue → Azure AI Vision API → Results Storage → Event Notification
```

#### 5. Gallery Viewer Component ✅ IMPLEMENTED
```csharp
// Services:
IGalleryService          // Gallery management and search
GalleryViewModel         // WPF MVVM view model
GalleryWindow           // WPF gallery interface

// Features:
WPF Gallery Interface → Real-time Updates → Search → Interactive Actions
```

#### 6. Domain Component ✅ IMPLEMENTED
```csharp
// Entities:
Screenshot              // Core business entity
BlobReference          // Value object for file references

// Business Logic:
Entity Creation → Validation → State Management
```

## Application Workflow

### Complete User Journey

```
1. Application Startup:
   ┌─ System Tray App Starts
   ├─ Global Hotkey Registered (Ctrl+Print Screen)
   ├─ Background Services Started (AI Queue)
   └─ Components Initialized

2. Screenshot Capture:
   ┌─ User: Ctrl+Print Screen
   ├─ Area Selection Overlay Appears
   ├─ User Selects Area
   ├─ Screenshot Captured
   ├─ Saved to Local Storage (immediate)
   ├─ Queued for AI Analysis (background)
   └─ Gallery Updated (real-time)

3. AI Analysis (Background):
   ┌─ Analysis Job Dequeued
   ├─ Azure AI Vision API Called
   ├─ Content Description Generated
   ├─ Results Stored Locally
   └─ Gallery Updated (real-time event)

4. Gallery Management:
   ┌─ User Opens Gallery (Tray → Show Gallery)
   ├─ Screenshots Displayed with AI Descriptions
   ├─ Search/Filter Available
   ├─ Interactive Actions (Open, Delete, etc.)
   └─ Real-time Updates as New Screenshots Added
```

## System Tray Application Design

### Main Application Structure
```csharp
public class ScreenshotManagerTrayApp : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NotifyIcon _trayIcon;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IAnalysisQueueService _analysisQueue;
    private GalleryWindow? _galleryWindow;

    // Application Lifecycle
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Configure dependency injection
        // 2. Initialize system tray icon
        // 3. Register global hotkey
        // 4. Start background services
        // 5. Hide main window (run in tray)
    }

    // System Tray Features
    private void CreateTrayIcon()
    {
        // Context Menu:
        // - 📸 Capture Screenshot (Ctrl+Print Screen)
        // - 🖼️ Show Gallery
        // - ⚙️ Settings
        // - ❌ Exit
    }

    // Global Hotkey Handler
    private async void OnScreenshotRequested()
    {
        // Execute complete capture workflow
        // Show toast notification on completion
    }
}
```

### System Tray Context Menu
```
┌─────────────────────────────────────┐
│ 📸 Capture Screenshot (Ctrl+PrtScn) │
├─────────────────────────────────────┤
│ 🖼️ Show Gallery                     │
├─────────────────────────────────────┤
│ 📊 View Statistics                  │
├─────────────────────────────────────┤
│ ⚙️ Settings                         │
├─────────────────────────────────────┤
│ ❓ Help                             │
├─────────────────────────────────────┤
│ ❌ Exit                             │
└─────────────────────────────────────┘
```

### Toast Notifications
```csharp
// Notification Examples:
"📸 Screenshot captured and saved"
"🤖 AI analysis completed: A person using a computer"
"❌ Screenshot capture cancelled"
"⚠️ AI analysis failed - check internet connection"
```

## Dependency Injection Configuration

### Service Registration
```csharp
public static class DependencyInjection
{
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Core Components
        services.AddDomainServices();
        services.AddLocalStorageServices(configuration);
        services.AddScreenshotCaptureServices();
        services.AddAzureVisionAnalysisServices(configuration);
        services.AddGalleryViewerServices();

        // System Tray Application
        services.AddSingleton<ScreenshotManagerTrayApp>();
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<NotificationService>();

        // Configuration
        services.Configure<AppSettings>(configuration.GetSection("App"));
        services.Configure<AzureVisionOptions>(configuration.GetSection("AzureVision"));

        return services;
    }
}
```

## Data Flow Architecture

### Screenshot Capture Flow
```
User Input (Ctrl+Print Screen)
    ↓
Global Hotkey Service
    ↓
Area Selection Service (UI Overlay)
    ↓
Screenshot Capture Service
    ↓
Local Storage Service (File System)
    ↓
Analysis Queue Service (Background)
    ↓
Gallery Service (Real-time Update)
```

### AI Analysis Flow
```
Analysis Queue (Background Service)
    ↓
Azure Vision Service (API Call)
    ↓
Vision Analysis Result
    ↓
Local Metadata Storage
    ↓
Gallery Update Event
    ↓
Gallery View Model (Real-time UI Update)
```

### Gallery Display Flow
```
Gallery Service
    ↓
Load Screenshots from Local Storage
    ↓
Load AI Results from Metadata Storage
    ↓
Create View Models
    ↓
Observable Collections (MVVM)
    ↓
WPF Gallery Interface
```

## Configuration Management

### Application Settings
```json
{
  "App": {
    "StartMinimized": true,
    "ShowToastNotifications": true,
    "AutoStartWithWindows": false,
    "HotkeyEnabled": true,
    "GalleryAutoRefresh": true
  },
  "AzureVision": {
    "Endpoint": "https://your-ai-foundry-endpoint.cognitiveservices.azure.com/",
    "ApiKey": "stored-in-user-secrets",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "IncludeDenseCaptions": true,
    "IncludeObjects": true,
    "IncludeText": true,
    "IncludeTags": true
  },
  "Storage": {
    "ScreenshotsDirectory": "~/Documents/Screenshots",
    "ThumbnailsDirectory": "~/Documents/Screenshots/Thumbnails",
    "MaxFileSizeBytes": 52428800,
    "ImageOptimization": {
      "MaxWidth": 1920,
      "MaxHeight": 1080,
      "JpegQuality": 85
    }
  }
}
```

## Security Considerations

### Privacy Protection
- **Local-Only Storage**: Screenshots never leave user's machine unless explicitly analyzed
- **Secure API Keys**: Azure credentials stored in user secrets or secure configuration
- **No Data Retention**: Cloud AI services configured not to store images
- **User Control**: Clear consent required for cloud AI analysis

### System Security
- **Global Hotkey**: Secure registration without interfering with other applications
- **File Permissions**: Appropriate access controls on screenshot directories
- **Process Isolation**: Components isolated through dependency injection
- **Error Handling**: Graceful failure without exposing sensitive information

## Performance Characteristics

### Responsiveness
- **Hotkey Response**: < 100ms from keypress to overlay display
- **Screenshot Capture**: < 500ms for typical screen areas
- **Gallery Loading**: < 1s for 100 screenshots with thumbnails
- **Search Response**: < 200ms for text-based searches

### Scalability
- **Background Processing**: Non-blocking AI analysis queue
- **Memory Management**: Efficient image handling and disposal
- **Storage Efficiency**: Optimized images and thumbnails
- **Event-Driven Updates**: Minimal UI refresh overhead

### Resource Usage
- **Idle State**: < 50MB RAM, minimal CPU usage
- **Active Capture**: Temporary spike during screenshot processing
- **AI Analysis**: Background processing with configurable concurrency
- **Gallery Display**: Lazy loading and virtualization for large collections

## Deployment Architecture

### Single Executable
```
ScreenshotManager.exe
├── All components bundled
├── Self-contained .NET 8 runtime
├── Configuration files
└── Default settings
```

### Installation Structure
```
%ProgramFiles%/ScreenshotManager/
├── ScreenshotManager.exe
├── appsettings.json
├── README.md
└── Components/
    ├── Screenshots/ (default storage)
    └── Thumbnails/

%APPDATA%/ScreenshotManager/
├── user-settings.json
├── user-secrets.json (encrypted API keys)
└── metadata.db (SQLite database)
```

## Future Extensions

### Stage 2 Enhancements (WALK)
- **Advanced Search**: Semantic search with local models
- **Smart Organization**: AI-powered automatic categorization
- **Workflow Detection**: Pattern recognition in screenshot sequences
- **Enhanced UI**: Timeline view, project grouping

### Stage 3 Enhancements (RUN)
- **Team Collaboration**: Shared galleries with access controls
- **Automation**: Scheduled captures, automated workflows
- **Integration**: API for third-party applications
- **Analytics**: Usage insights and productivity metrics

---

**Last Updated**: June 29, 2025  
**Version**: 2.0 (Current Implementation)  
**Status**: Core Components Complete, System Tray Integration Pending