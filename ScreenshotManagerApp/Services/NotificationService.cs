using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenshotManagerApp.Configuration;

namespace ScreenshotManagerApp.Services;

public class NotificationService : INotificationService
{
    private readonly ITrayIconService _trayIconService;
    private readonly ILogger<NotificationService> _logger;
    private readonly AppSettings _settings;

    public NotificationService(
        ITrayIconService trayIconService,
        ILogger<NotificationService> logger,
        IOptions<AppSettings> settings)
    {
        _trayIconService = trayIconService;
        _logger = logger;
        _settings = settings.Value;
    }

    public void ShowSuccess(string message)
    {
        if (_settings.ShowToastNotifications)
        {
            _trayIconService.ShowNotification("Screenshot Manager", message, NotificationType.Success);
        }
        _logger.LogInformation("Success: {Message}", message);
    }

    public void ShowError(string message)
    {
        if (_settings.ShowToastNotifications)
        {
            _trayIconService.ShowNotification("Screenshot Manager - Error", message, NotificationType.Error);
        }
        _logger.LogError("Error notification: {Message}", message);
    }

    public void ShowInfo(string message)
    {
        if (_settings.ShowToastNotifications)
        {
            _trayIconService.ShowNotification("Screenshot Manager", message, NotificationType.Info);
        }
        _logger.LogInformation("Info: {Message}", message);
    }

    public void ShowWarning(string message)
    {
        if (_settings.ShowToastNotifications)
        {
            _trayIconService.ShowNotification("Screenshot Manager - Warning", message, NotificationType.Warning);
        }
        _logger.LogWarning("Warning notification: {Message}", message);
    }

    public void ShowAnalysisComplete(string description)
    {
        var shortDescription = description.Length > 50 
            ? description[..50] + "..." 
            : description;
        
        ShowInfo($"🤖 Analysis complete: {shortDescription}");
    }
}