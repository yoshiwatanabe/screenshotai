using AzureVisionAnalysis.Services;
using GalleryViewer.Services;
using GalleryViewer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenshotCapture.Services;
using ScreenshotManagerApp.Configuration;
using ScreenshotManagerApp.Services;
using System.Windows;

namespace ScreenshotManagerApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<App> _logger;
    private readonly ITrayIconService _trayIconService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly INotificationService _notificationService;
    private readonly IAnalysisQueueService _analysisQueueService;
    private readonly IScreenshotCaptureService _captureService;
    private readonly IGalleryService _galleryService;

    private GalleryWindow? _galleryWindow;

    public App(
        IServiceProvider serviceProvider,
        ILogger<App> logger,
        ITrayIconService trayIconService,
        IGlobalHotkeyService hotkeyService,
        INotificationService notificationService,
        IAnalysisQueueService analysisQueueService,
        IScreenshotCaptureService captureService,
        IGalleryService galleryService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _trayIconService = trayIconService;
        _hotkeyService = hotkeyService;
        _notificationService = notificationService;
        _analysisQueueService = analysisQueueService;
        _captureService = captureService;
        _galleryService = galleryService;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _logger.LogInformation("Starting Screenshot Manager application...");

            // Hide the default main window - we run in system tray
            if (MainWindow != null)
            {
                MainWindow.WindowState = WindowState.Minimized;
                MainWindow.ShowInTaskbar = false;
                MainWindow.Visibility = Visibility.Hidden;
            }

            // Initialize system tray
            _trayIconService.Initialize();
            _trayIconService.MenuItemClicked += OnTrayMenuItemClicked;

            // Register global hotkey
            var hotkeyRegistered = _hotkeyService.RegisterCaptureHotkey(OnScreenshotRequested);
            if (hotkeyRegistered)
            {
                _logger.LogInformation("Global hotkey registered: {Hotkey}", _hotkeyService.GetHotkeyDisplayString());
                _notificationService.ShowInfo($"Screenshot Manager started. Press {_hotkeyService.GetHotkeyDisplayString()} to capture.");
            }
            else
            {
                _logger.LogWarning("Failed to register global hotkey");
                _notificationService.ShowWarning("Failed to register global hotkey. You can still use the tray menu to capture screenshots.");
            }

            // Start background services
            await _analysisQueueService.StartProcessingAsync();
            _logger.LogInformation("Background analysis service started");

            // Subscribe to analysis completion events
            _analysisQueueService.AnalysisCompleted += OnAnalysisCompleted;

            _logger.LogInformation("Screenshot Manager startup complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application startup");
            _notificationService.ShowError("Failed to start Screenshot Manager. Check logs for details.");
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger.LogInformation("Shutting down Screenshot Manager...");

            // Unregister global hotkey
            _hotkeyService.UnregisterCaptureHotkey();

            // Stop background services
            await _analysisQueueService.StopProcessingAsync();

            // Dispose system tray
            _trayIconService.Dispose();

            _logger.LogInformation("Screenshot Manager shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application shutdown");
        }

        base.OnExit(e);
    }

    private async void OnScreenshotRequested()
    {
        try
        {
            _logger.LogDebug("Screenshot capture requested via hotkey");
            
            var result = await _captureService.PerformCompleteCaptureWorkflowAsync();
            
            if (result.Success && result.ImageData != null)
            {
                _logger.LogInformation("Screenshot captured successfully: {FileName}", result.FileName);
                _notificationService.ShowSuccess($"📸 Screenshot saved: {Path.GetFileName(result.FileName)}");
                
                // Queue for AI analysis
                if (!string.IsNullOrEmpty(result.FileName))
                {
                    var screenshotId = Guid.NewGuid(); // In real app, this would come from domain service
                    await _analysisQueueService.QueueAnalysisAsync(screenshotId, result.FileName);
                    _logger.LogDebug("Screenshot queued for AI analysis: {ScreenshotId}", screenshotId);
                }
            }
            else
            {
                _logger.LogWarning("Screenshot capture failed: {Error}", result.ErrorMessage);
                _notificationService.ShowError($"❌ Capture failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during screenshot capture");
            _notificationService.ShowError("Screenshot capture failed. Please try again.");
        }
    }

    private void OnAnalysisCompleted(object? sender, AzureVisionAnalysis.Services.AnalysisCompletedEventArgs e)
    {
        try
        {
            if (e.Success && !string.IsNullOrEmpty(e.Result.MainCaption))
            {
                _logger.LogInformation("AI analysis completed for screenshot {ScreenshotId}: {Caption}", 
                    e.ScreenshotId, e.Result.MainCaption);
                
                var shortDescription = e.Result.MainCaption.Length > 50 
                    ? e.Result.MainCaption[..50] + "..." 
                    : e.Result.MainCaption;
                
                _notificationService.ShowInfo($"🤖 Analysis complete: {shortDescription}");
            }
            else
            {
                _logger.LogWarning("AI analysis failed for screenshot {ScreenshotId}: {Error}", 
                    e.ScreenshotId, e.Result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling analysis completion event");
        }
    }

    private async void OnTrayMenuItemClicked(object? sender, TrayMenuItemEventArgs e)
    {
        try
        {
            switch (e.MenuItemId)
            {
                case "capture":
                    OnScreenshotRequested();
                    break;
                    
                case "gallery":
                    ShowGallery();
                    break;
                    
                case "statistics":
                    await ShowStatisticsAsync();
                    break;
                    
                case "settings":
                    ShowSettings();
                    break;
                    
                case "help":
                    ShowHelp();
                    break;
                    
                case "exit":
                    Shutdown();
                    break;
                    
                default:
                    _logger.LogWarning("Unknown tray menu item clicked: {MenuItemId}", e.MenuItemId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tray menu click: {MenuItemId}", e.MenuItemId);
        }
    }

    private void ShowGallery()
    {
        try
        {
            if (_galleryWindow == null || !_galleryWindow.IsLoaded)
            {
                _galleryWindow = _serviceProvider.GetRequiredService<GalleryWindow>();
                _galleryWindow.Closed += (s, e) => _galleryWindow = null;
            }

            if (_galleryWindow.WindowState == WindowState.Minimized)
            {
                _galleryWindow.WindowState = WindowState.Normal;
            }

            _galleryWindow.Show();
            _galleryWindow.Activate();
            _galleryWindow.Focus();

            _logger.LogDebug("Gallery window opened");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening gallery window");
            _notificationService.ShowError("Failed to open gallery. Please try again.");
        }
    }

    private async Task ShowStatisticsAsync()
    {
        try
        {
            var stats = await _galleryService.GetGalleryStatisticsAsync();
            var queueStatus = await _analysisQueueService.GetQueueStatusAsync();
            
            var message = $"📊 Statistics:\n" +
                         $"Total Screenshots: {stats.TotalScreenshots}\n" +
                         $"Analyzed: {stats.AnalyzedScreenshots}\n" +
                         $"Pending Analysis: {queueStatus.QueuedJobs + queueStatus.ProcessingJobs}\n" +
                         $"Storage Used: {FormatFileSize(stats.TotalStorageBytes)}";
            
            MessageBox.Show(message, "Screenshot Manager - Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing statistics");
            _notificationService.ShowError("Failed to load statistics.");
        }
    }

    private void ShowSettings()
    {
        _notificationService.ShowInfo("Settings functionality coming soon!");
        // TODO: Implement settings dialog
    }

    private void ShowHelp()
    {
        var helpMessage = $"Screenshot Manager\n\n" +
                         $"🔧 Hotkey: {_hotkeyService.GetHotkeyDisplayString()}\n" +
                         $"📸 Right-click tray icon for menu\n" +
                         $"🖼️ View gallery to manage screenshots\n" +
                         $"🤖 AI analysis runs automatically\n\n" +
                         $"For more help, visit the project repository.";
        
        MessageBox.Show(helpMessage, "Screenshot Manager - Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
        return $"{bytes / (1024 * 1024 * 1024):F1} GB";
    }
}