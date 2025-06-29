using Microsoft.Extensions.DependencyInjection;
using ScreenshotCapture.Services;

namespace ScreenshotCapture.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ScreenshotCapture services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddScreenshotCaptureServices(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddScoped<IAreaSelectionService, AreaSelectionService>();
        services.AddScoped<IScreenshotCaptureService, ScreenshotCaptureService>();

        return services;
    }
}