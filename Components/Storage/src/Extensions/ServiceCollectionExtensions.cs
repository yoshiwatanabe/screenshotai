using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<ILocalStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());
        services.AddScoped<IScreenshotStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());

        return services;
    }
}