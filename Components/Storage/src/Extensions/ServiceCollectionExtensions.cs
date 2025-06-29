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
        services.Configure<StorageOptions>(options => configuration.GetSection("Storage").Bind(options));
        services.AddScoped<ILocalStorageService, LocalFileStorageService>();
        
        return services;
    }
}