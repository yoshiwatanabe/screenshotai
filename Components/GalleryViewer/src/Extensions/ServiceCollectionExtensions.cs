using GalleryViewer.Services;
using GalleryViewer.ViewModels;
using GalleryViewer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryViewer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Gallery Viewer services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGalleryViewerServices(this IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IGalleryService, GalleryService>();
        
        // Register view models
        services.AddTransient<GalleryViewModel>();
        
        // Register views
        services.AddTransient<GalleryWindow>();

        return services;
    }
}