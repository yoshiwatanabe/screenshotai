using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vision.Configuration;
using Vision.Services;

namespace Vision.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVisionServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AzureVisionOptions>()
                .Bind(configuration.GetSection("AZURE_VISION"));

        services.AddHttpClient<AzureVisionHttpService>()
                .ConfigureHttpClient((serviceProvider, httpClient) =>
                {
                    var visionOptions = serviceProvider.GetRequiredService<IOptions<AzureVisionOptions>>().Value;
                    httpClient.BaseAddress = new Uri(visionOptions.Endpoint);
                    httpClient.Timeout = TimeSpan.FromSeconds(visionOptions.TimeoutSeconds);
                    serviceProvider.GetRequiredService<ILoggerFactory>()
                                   .CreateLogger("Vision.HttpClient")
                                   .LogDebug($"HttpClient BaseAddress set to: {httpClient.BaseAddress}");
                });
        services.AddScoped<AzureVisionHttpService>();
        
        return services;
    }
}