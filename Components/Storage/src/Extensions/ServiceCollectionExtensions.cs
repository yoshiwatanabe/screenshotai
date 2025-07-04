using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Storage.Configuration;
using Storage.Services;

namespace Storage.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<AzureVisionOptions>(configuration.GetSection("Storage:AzureVision"));

        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<ILocalStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());
        services.AddScoped<IScreenshotStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());
        
        services.AddHttpClient<AzureVisionHttpService>((serviceProvider, httpClient) =>
        {
            var storageOptions = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
            httpClient.Timeout = TimeSpan.FromSeconds(storageOptions.AzureVision.TimeoutSeconds);
        });
        services.AddScoped<AzureVisionHttpService>();
        
        return services;
    }
}