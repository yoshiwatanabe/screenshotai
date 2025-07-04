# Screenshot Manager - Setup Instructions

## Overview

The Screenshot Manager now uses a **Console App + Task Scheduler** approach instead of a system tray application for easier deployment and fewer Windows-specific build issues.

## Architecture

- **ScreenshotConsoleService**: Background console application that runs continuously
  - Registers global hotkey (Ctrl+Print Screen)
  - Captures full-screen screenshots
  - Saves screenshots locally to `Documents/Screenshots`
  - Provides console commands for manual testing
  
- **GalleryLauncher**: Standalone WPF application
  - Views saved screenshots in a gallery format
  - Can be launched independently when needed
  - Refreshes automatically to show new screenshots

## Installation Steps

### 1. Build the Applications

On a Windows machine with .NET 8 SDK:

```bash
# Build the console service
dotnet publish ScreenshotConsoleService/ScreenshotConsoleService.csproj -c Release -r win-x64 --self-contained

# Build the gallery launcher
dotnet publish GalleryLauncher/GalleryLauncher.csproj -c Release -r win-x64 --self-contained
```

### 2. Deploy the Applications

Copy the published applications to a permanent location:

```
C:\Program Files\ScreenshotManager\
├── ScreenshotConsoleService.exe
├── GalleryLauncher.exe
└── ... (other files)
```

### 3. Configure Task Scheduler

1. Open **Task Scheduler** (`taskschd.msc`)
2. Click **Create Task...** (not "Create Basic Task")
3. Configure the task:

#### General Tab
- **Name**: Screenshot Manager Service
- **Description**: Background service for screenshot capture
- **Security options**: 
  - ☑️ Run whether user is logged on or not
  - ☑️ Run with highest privileges
  - ☑️ Hidden

#### Triggers Tab
- Click **New...**
- **Begin the task**: At startup
- ☑️ Enabled

#### Actions Tab
- Click **New...**
- **Action**: Start a program
- **Program/script**: `C:\Program Files\ScreenshotManager\ScreenshotConsoleService.exe`
- **Start in**: `C:\Program Files\ScreenshotManager\`

#### Conditions Tab
- ☐ Start the task only if the computer is on AC power
- ☐ Stop if the computer switches to battery power

#### Settings Tab
- ☑️ Allow task to be run on demand
- ☑️ Run task as soon as possible after a scheduled start is missed
- ☑️ If the running task does not end when requested, force it to stop
- **If the task is already running**: Do not start a new instance

### 4. Start the Service

You can start the service in several ways:

**Option A: Start via Task Scheduler**
1. In Task Scheduler, right-click "Screenshot Manager Service"
2. Click "Run"

**Option B: Start manually for testing**
1. Open Command Prompt as Administrator
2. Navigate to the installation directory
3. Run `ScreenshotConsoleService.exe`

## Usage

### Taking Screenshots

- **Global Hotkey**: Press `Ctrl + Print Screen` from any application
- **Manual Capture**: If the console is visible, press `c` and Enter
- **Fallback Hotkey**: `Win + Shift + C` (if primary hotkey fails)

### Viewing Screenshots

1. Run `GalleryLauncher.exe` directly
2. The gallery will show all screenshots from `Documents/Screenshots`
3. Use the **Refresh** button to update the gallery
4. Use **Open Folder** to access the screenshots directory directly

### Console Commands

When the console service is running, you can use these commands:

- `c` - Capture screenshot manually
- `s` - Show service status
- `h` - Show help
- `Ctrl+C` - Stop service gracefully

## Configuration

Both applications use `appsettings.json` files for configuration:

### ScreenshotConsoleService/appsettings.json

- `HotkeyEnabled`: Enable/disable global hotkey
- `ScreenshotsDirectory`: Where to save screenshots
- `ShowCaptureMessages`: Show capture feedback in console
- `AutoAnalyzeScreenshots`: Enable AI analysis (placeholder)

### GalleryLauncher/appsettings.json

- `ScreenshotsDirectory`: Must match the console service directory
- `ThumbnailsDirectory`: Where thumbnails are cached
- Thumbnail and optimization settings

## Troubleshooting

### Service Not Starting
1. Check Task Scheduler event logs
2. Ensure the executable path is correct
3. Verify permissions (run as administrator)

### Global Hotkey Not Working
1. Check if another application is using the same hotkey
2. Try the fallback hotkey (`Win + Shift + C`)
3. Use manual capture (`c` in console) for testing

### Screenshots Not Appearing in Gallery
1. Check that both apps use the same `ScreenshotsDirectory`
2. Click **Refresh** in the gallery
3. Verify directory permissions

### Console Service High CPU Usage
- This should not happen with the current implementation
- If it occurs, restart the service via Task Scheduler

## File Locations

- **Screenshots**: `%USERPROFILE%\Documents\Screenshots\`
- **Thumbnails**: `%USERPROFILE%\Documents\Screenshots\Thumbnails\`
- **Logs**: Console output (can be redirected to file if needed)

## Uninstallation

1. Stop the scheduled task in Task Scheduler
2. Delete the task from Task Scheduler
3. Delete the application files from `C:\Program Files\ScreenshotManager\`
4. Optionally delete screenshots from `Documents/Screenshots`

## Future Enhancements

- Azure AI Vision integration for automatic screenshot analysis
- Area selection capture (would require additional UI component)
- Notification system when screenshots are captured
- Automatic cleanup of old screenshots
- Export functionality for screenshots