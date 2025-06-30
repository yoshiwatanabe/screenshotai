using AzureVisionAnalysis.Extensions;
using GalleryViewer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScreenshotCapture.Extensions;
using ScreenshotManagerApp.Services;
using Storage.Extensions;

namespace ScreenshotManagerApp.Configuration;

public static class DependencyInjection
{
    /// <summary>
    /// Configures all application services in the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection ConfigureApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure application settings
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

        // Register all component services
        services.AddDomainServices();
        services.AddLocalStorageServices(configuration);
        services.AddScreenshotCaptureServices();
        services.AddAzureVisionAnalysisServices(configuration);
        services.AddGalleryViewerServices();

        // Register main application services
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Register the main WPF application
        services.AddSingleton<App>();

        return services;
    }

    /// <summary>
    /// Adds domain services (currently no specific registration needed)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Domain services are pure entities and value objects
        // No specific service registration needed at this time
        return services;
    }
}