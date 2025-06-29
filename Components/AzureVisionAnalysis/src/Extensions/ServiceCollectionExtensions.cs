using AzureVisionAnalysis.Configuration;
using AzureVisionAnalysis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureVisionAnalysis.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Vision Analysis services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration containing Azure Vision settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAzureVisionAnalysisServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<AzureVisionOptions>(configuration.GetSection(AzureVisionOptions.SectionName));
        
        // Register services
        services.AddSingleton<IAzureVisionService, AzureVisionService>();
        services.AddSingleton<IAnalysisQueueService, AnalysisQueueService>();
        
        // Register the queue service as a hosted service for background processing
        services.AddHostedService(provider => (AnalysisQueueService)provider.GetRequiredService<IAnalysisQueueService>());
        
        return services;
    }

    /// <summary>
    /// Adds Azure Vision Analysis services with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure Azure Vision options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAzureVisionAnalysisServices(
        this IServiceCollection services,
        Action<AzureVisionOptions> configureOptions)
    {
        // Configure options using the provided action
        services.Configure(configureOptions);
        
        // Register services
        services.AddSingleton<IAzureVisionService, AzureVisionService>();
        services.AddSingleton<IAnalysisQueueService, AnalysisQueueService>();
        
        // Register the queue service as a hosted service for background processing
        services.AddHostedService(provider => (AnalysisQueueService)provider.GetRequiredService<IAnalysisQueueService>());
        
        return services;
    }

    /// <summary>
    /// Validates Azure Vision configuration and ensures service availability
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if Azure Vision is properly configured and available</returns>
    public static async Task<bool> ValidateAzureVisionConfigurationAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var visionService = serviceProvider.GetRequiredService<IAzureVisionService>();
            return await visionService.IsServiceAvailableAsync(cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }
}