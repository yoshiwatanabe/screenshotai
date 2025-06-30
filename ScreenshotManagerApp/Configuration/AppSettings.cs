namespace ScreenshotManagerApp.Configuration;

public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>
    /// Whether to start the application minimized to system tray
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Whether to show toast notifications
    /// </summary>
    public bool ShowToastNotifications { get; set; } = true;

    /// <summary>
    /// Whether to auto-start with Windows
    /// </summary>
    public bool AutoStartWithWindows { get; set; } = false;

    /// <summary>
    /// Whether the global hotkey is enabled
    /// </summary>
    public bool HotkeyEnabled { get; set; } = true;

    /// <summary>
    /// Display text for the hotkey in UI
    /// </summary>
    public string HotkeyDisplayText { get; set; } = "Ctrl+Print Screen";

    /// <summary>
    /// Whether the gallery should auto-refresh when new screenshots are added
    /// </summary>
    public bool GalleryAutoRefresh { get; set; } = true;

    /// <summary>
    /// Timeout for toast notifications in seconds
    /// </summary>
    public int NotificationTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Whether to show debug information in notifications
    /// </summary>
    public bool ShowDebugInfo { get; set; } = false;

    /// <summary>
    /// Maximum number of screenshots to keep in memory for quick access
    /// </summary>
    public int MaxCachedScreenshots { get; set; } = 100;

    /// <summary>
    /// Whether to automatically queue new screenshots for AI analysis
    /// </summary>
    public bool AutoAnalyzeScreenshots { get; set; } = true;

    /// <summary>
    /// Application version for display purposes
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Application name for display purposes
    /// </summary>
    public string ApplicationName { get; set; } = "Screenshot Manager";
}