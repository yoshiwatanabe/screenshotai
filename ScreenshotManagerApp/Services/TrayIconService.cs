using Microsoft.Extensions.Logging;
using ScreenshotManagerApp.Configuration;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Options;

namespace ScreenshotManagerApp.Services;

public class TrayIconService : ITrayIconService
{
    private readonly ILogger<TrayIconService> _logger;
    private readonly AppSettings _settings;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _disposed;

    public event EventHandler<TrayMenuItemEventArgs>? MenuItemClicked;

    public TrayIconService(ILogger<TrayIconService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public void Initialize()
    {
        try
        {
            _logger.LogDebug("Initializing system tray icon");

            // Create the NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = LoadAppIcon(),
                Text = "Screenshot Manager",
                Visible = true
            };

            // Create context menu
            CreateContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;

            // Handle double-click to show gallery
            _notifyIcon.DoubleClick += (s, e) => OnMenuItemClick("gallery", "Show Gallery");

            _logger.LogInformation("System tray icon initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system tray icon");
            throw;
        }
    }

    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        if (_notifyIcon == null || !_settings.ShowToastNotifications)
            return;

        try
        {
            var balloonIcon = type switch
            {
                NotificationType.Success => ToolTipIcon.Info,
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            _notifyIcon.ShowBalloonTip(
                _settings.NotificationTimeoutSeconds * 1000,
                title,
                message,
                balloonIcon);

            _logger.LogDebug("Showed notification: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification");
        }
    }

    public void UpdateIcon(string iconPath)
    {
        if (_notifyIcon == null)
            return;

        try
        {
            if (File.Exists(iconPath))
            {
                _notifyIcon.Icon = new Icon(iconPath);
                _logger.LogDebug("Updated tray icon: {IconPath}", iconPath);
            }
            else
            {
                _logger.LogWarning("Icon file not found: {IconPath}", iconPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tray icon");
        }
    }

    private void CreateContextMenu()
    {
        _contextMenu = new ContextMenuStrip();

        // Add menu items
        AddMenuItem("capture", "📸 Capture Screenshot (Ctrl+PrtScn)", true);
        AddSeparator();
        AddMenuItem("gallery", "🖼️ Show Gallery", true);
        AddMenuItem("statistics", "📊 View Statistics", true);
        AddSeparator();
        AddMenuItem("settings", "⚙️ Settings", true);
        AddMenuItem("help", "❓ Help", true);
        AddSeparator();
        AddMenuItem("exit", "❌ Exit", true);

        _logger.LogDebug("Context menu created with {Count} items", _contextMenu.Items.Count);
    }

    private void AddMenuItem(string id, string text, bool enabled = true)
    {
        if (_contextMenu == null)
            return;

        var menuItem = new ToolStripMenuItem(text)
        {
            Tag = id,
            Enabled = enabled
        };

        menuItem.Click += (s, e) => OnMenuItemClick(id, text);
        _contextMenu.Items.Add(menuItem);
    }

    private void AddSeparator()
    {
        _contextMenu?.Items.Add(new ToolStripSeparator());
    }

    private void OnMenuItemClick(string id, string text)
    {
        try
        {
            _logger.LogDebug("Tray menu item clicked: {Id} - {Text}", id, text);
            MenuItemClicked?.Invoke(this, new TrayMenuItemEventArgs { MenuItemId = id, MenuItemText = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling menu item click: {Id}", id);
        }
    }

    private Icon LoadAppIcon()
    {
        try
        {
            // Try to load from resources first
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app-icon.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            // Fallback to embedded resource or default
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "ScreenshotManagerApp.Resources.app-icon.ico";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                return new Icon(stream);
            }

            // Ultimate fallback - create a simple icon
            return CreateDefaultIcon();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load app icon, using default");
            return CreateDefaultIcon();
        }
    }

    private Icon CreateDefaultIcon()
    {
        // Create a simple 16x16 icon programmatically
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Draw a simple camera icon
        graphics.Clear(Color.Transparent);
        graphics.FillRectangle(Brushes.DarkBlue, 2, 4, 12, 8);
        graphics.FillEllipse(Brushes.LightBlue, 5, 6, 6, 4);
        graphics.FillEllipse(Brushes.White, 7, 7, 2, 2);
        
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _logger.LogDebug("Disposing system tray icon");

                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }

                _contextMenu?.Dispose();
                _contextMenu = null;

                _logger.LogDebug("System tray icon disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing system tray icon");
            }

            _disposed = true;
        }
    }
}