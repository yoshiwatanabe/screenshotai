namespace ScreenshotManagerApp.Services;

public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Initializes the system tray icon and context menu
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shows a notification from the system tray
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message</param>
    /// <param name="type">Type of notification</param>
    void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);

    /// <summary>
    /// Updates the tray icon
    /// </summary>
    /// <param name="iconPath">Path to the icon file</param>
    void UpdateIcon(string iconPath);

    /// <summary>
    /// Event fired when a context menu item is clicked
    /// </summary>
    event EventHandler<TrayMenuItemEventArgs>? MenuItemClicked;
}

public class TrayMenuItemEventArgs : EventArgs
{
    public string MenuItemId { get; set; } = string.Empty;
    public string MenuItemText { get; set; } = string.Empty;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}