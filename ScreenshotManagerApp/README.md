# Screenshot Manager - System Tray Application

## Overview

The main system tray application that integrates all Screenshot Manager components into a complete desktop application. This application runs in the Windows system tray and provides global hotkey capture functionality with Azure AI-powered analysis.

## Features

### System Tray Integration
- **System Tray Icon**: Always available in the Windows system tray
- **Context Menu**: Right-click access to all functionality
- **Double-Click**: Quick access to gallery window
- **Toast Notifications**: Real-time feedback for all operations

### Global Hotkey Capture
- **Primary**: `Ctrl+Print Screen` for screenshot capture
- **Fallback**: `Win+Shift+C` if primary hotkey fails to register
- **Area Selection**: Interactive overlay for selecting screen regions
- **Instant Save**: Screenshots saved immediately to local storage

### AI-Powered Analysis
- **Background Processing**: AI analysis runs without blocking capture
- **Azure AI Vision**: Comprehensive content description
- **Real-Time Updates**: Gallery updates automatically when analysis completes
- **Error Handling**: Graceful handling of network or API issues

### Gallery Management
- **WPF Interface**: Rich desktop gallery for viewing and managing screenshots
- **Real-Time Search**: Multi-field search across filenames, descriptions, and OCR text
- **Interactive Actions**: Open, delete, show in explorer, copy path
- **Statistics**: View analysis progress and storage usage

## Application Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 System Tray Application                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Tray Icon     │  │  Notification   │  │   App       │  │
│  │   Service       │  │    Service      │  │ Lifecycle   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
    ┌─────────▼─────────┐ ┌───▼────┐ ┌────────▼─────────┐
    │  Screenshot       │ │Gallery │ │  Azure Vision    │
    │   Capture         │ │Viewer  │ │   Analysis       │
    └─────────┬─────────┘ └────────┘ └──────────────────┘
              │                              │
    ┌─────────▼─────────┐           ┌────────▼─────────┐
    │   Local Storage   │           │     Domain       │
    └───────────────────┘           └──────────────────┘
```

## User Workflow

### Complete Screenshot Workflow
1. **Trigger**: User presses `Ctrl+Print Screen`
2. **Selection**: Area selection overlay appears
3. **Capture**: User selects area and screenshot is captured
4. **Save**: Image immediately saved to local storage with optimization
5. **Notify**: Toast notification confirms successful capture
6. **Queue**: Screenshot queued for background AI analysis
7. **Analyze**: Azure AI Vision analyzes image content
8. **Update**: Gallery updates with AI description
9. **Complete**: Second notification shows analysis results

### System Tray Menu
```
┌─────────────────────────────────────┐
│ 📸 Capture Screenshot (Ctrl+PrtScn) │ ← Trigger manual capture
├─────────────────────────────────────┤
│ 🖼️ Show Gallery                     │ ← Open gallery window
├─────────────────────────────────────┤
│ 📊 View Statistics                  │ ← Show quick stats
├─────────────────────────────────────┤
│ ⚙️ Settings                         │ ← Open settings (planned)
├─────────────────────────────────────┤
│ ❓ Help                             │ ← Show help information
├─────────────────────────────────────┤
│ ❌ Exit                             │ ← Exit application
└─────────────────────────────────────┘
```

## Configuration

### Application Settings (`appsettings.json`)
```json
{
  "App": {
    "StartMinimized": true,
    "ShowToastNotifications": true,
    "AutoStartWithWindows": false,
    "HotkeyEnabled": true,
    "GalleryAutoRefresh": true,
    "NotificationTimeoutSeconds": 5,
    "AutoAnalyzeScreenshots": true
  }
}
```

### Required Configuration
- **Azure AI Vision**: API key and endpoint in user secrets
- **Storage**: Local directories for screenshots and thumbnails
- **Logging**: Configurable log levels for all components

### User Secrets Setup
```bash
# Set Azure AI Vision credentials
dotnet user-secrets set "AzureVision:ApiKey" "your-api-key"
dotnet user-secrets set "AzureVision:Endpoint" "https://your-endpoint.cognitiveservices.azure.com/"
```

## Dependencies

### Component Dependencies
- **Domain**: Core entities and business logic
- **Storage**: Local file storage with image optimization
- **ScreenshotCapture**: Global hotkey and area selection
- **AzureVisionAnalysis**: Background AI processing queue
- **GalleryViewer**: WPF gallery interface

### Framework Dependencies
- **.NET 8**: Latest runtime features and performance
- **WPF**: Rich desktop interface framework
- **Windows Forms**: System tray and notification integration
- **Microsoft Extensions**: Hosting, DI, configuration, logging

### External Dependencies
- **Azure AI Vision**: Cloud-based image analysis
- **CommunityToolkit.Mvvm**: Modern MVVM framework
- **SixLabors.ImageSharp**: High-performance image processing

## Building and Running

### Prerequisites
- .NET 8 SDK
- Windows 10/11 (for system tray and global hotkey support)
- Azure AI Vision API key (for AI analysis features)

### Build Instructions
```bash
# Navigate to application directory
cd ScreenshotManagerApp

# Restore dependencies
dotnet restore

# Build application
dotnet build

# Run application
dotnet run
```

### Release Build
```bash
# Create self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true

# Output will be in bin/Release/net8.0-windows/win-x64/publish/
```

## Usage

### First Time Setup
1. **Start Application**: Run ScreenshotManagerApp.exe
2. **System Tray**: Look for camera icon in system tray
3. **Test Hotkey**: Press `Ctrl+Print Screen` to test capture
4. **Configure AI**: Set Azure AI Vision credentials in user secrets
5. **Take Screenshots**: Use hotkey or tray menu to capture

### Daily Usage
1. **Background Operation**: Application runs silently in system tray
2. **Quick Capture**: Press `Ctrl+Print Screen` anywhere in Windows
3. **Select Area**: Drag to select the area you want to capture
4. **Automatic Processing**: Screenshot saved and analyzed automatically
5. **View Results**: Double-click tray icon to open gallery and see AI descriptions

## Troubleshooting

### Common Issues

#### Hotkey Not Working
- **Check Registration**: Look for error in notification or logs
- **Conflict**: Another application may be using the same hotkey
- **Fallback**: Try `Win+Shift+C` if primary hotkey fails
- **Manual Capture**: Use tray menu "Capture Screenshot" option

#### AI Analysis Failing
- **Internet Connection**: Ensure internet access for Azure API
- **API Key**: Verify Azure AI Vision credentials in user secrets
- **Service Status**: Check Azure AI Vision service status
- **Logs**: Check application logs for detailed error information

#### Gallery Not Opening
- **WPF Support**: Ensure .NET 8 desktop runtime is installed
- **Permissions**: Check file system permissions for screenshot directory
- **Memory**: Ensure sufficient memory for WPF application

### Logging
Application logs are written to console and can be redirected to files:
```bash
# Run with file logging
dotnet run > screenshot-manager.log 2>&1
```

Log levels can be configured in `appsettings.json` for different components.

## Architecture Benefits

### Privacy-First Design
- **Local Storage**: All screenshots stored on user's machine
- **Optional Cloud**: AI analysis only when user captures screenshots
- **No Data Retention**: Azure AI Vision configured not to store images
- **User Control**: Complete control over when and what gets analyzed

### Performance Optimized
- **Background Processing**: AI analysis never blocks screenshot capture
- **Efficient Storage**: Image optimization and thumbnail generation
- **Memory Management**: Proper disposal and resource cleanup
- **Fast Startup**: Quick system tray initialization

### User Experience
- **Non-Intrusive**: Runs silently until needed
- **Instant Feedback**: Toast notifications for all operations
- **Keyboard Focused**: Global hotkey for rapid access
- **Visual Management**: Rich gallery for organizing screenshots

## Future Enhancements

### Planned Features
- **Settings Dialog**: GUI for configuring all application options
- **Auto-Start**: Windows startup integration
- **Advanced Hotkeys**: Configurable hotkey combinations
- **Export Features**: Bulk export and sharing capabilities
- **Advanced AI**: Local AI models for offline operation

### Integration Opportunities
- **Team Sharing**: Shared galleries and collaboration features
- **Workflow Integration**: API for third-party applications
- **Cloud Sync**: Optional cloud backup and sync
- **Advanced Search**: Semantic search with local AI models