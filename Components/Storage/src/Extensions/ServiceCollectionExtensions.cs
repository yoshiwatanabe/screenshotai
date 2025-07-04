using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Storage.Configuration;
using Storage.Services;
using DotNetEnv;

namespace Storage.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load environment variables from .env file if it exists
        LoadEnvironmentVariables();

        // Configure storage options with environment variable overrides
        services.Configure<StorageOptions>(options =>
        {
            configuration.GetSection("Storage").Bind(options);
            
            // Override with environment variables if present
            OverrideWithEnvironmentVariables(options);
        });

        services.AddScoped<LocalFileStorageService>();
        services.AddScoped<ILocalStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());
        services.AddScoped<IScreenshotStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());
        
        // Add HttpClient for Azure Vision API with timeout configuration
        services.AddHttpClient<AzureVisionHttpService>((serviceProvider, httpClient) =>
        {
            var storageOptions = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
            httpClient.Timeout = TimeSpan.FromSeconds(storageOptions.AzureVision.TimeoutSeconds);
        });
        services.AddScoped<AzureVisionHttpService>();
        
        return services;
    }

    private static void LoadEnvironmentVariables()
    {
        try
        {
            // Try to load .env file from the current directory or parent directories
            var currentDir = Directory.GetCurrentDirectory();
            var envFile = FindEnvFile(currentDir);
            
            if (envFile != null)
            {
                Env.Load(envFile);
            }
        }
        catch
        {
            // Silently ignore if .env file is not found or cannot be loaded
        }
    }

    private static string? FindEnvFile(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        
        while (dir != null)
        {
            var envFile = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envFile))
            {
                return envFile;
            }
            dir = dir.Parent;
        }
        
        return null;
    }

    private static void OverrideWithEnvironmentVariables(StorageOptions options)
    {
        // Override Azure Vision settings with environment variables
        var endpoint = Environment.GetEnvironmentVariable("AZURE_VISION_ENDPOINT");
        if (!string.IsNullOrEmpty(endpoint))
        {
            options.AzureVision.Endpoint = endpoint;
        }

        var apiKey = Environment.GetEnvironmentVariable("AZURE_VISION_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            options.AzureVision.ApiKey = apiKey;
        }

        var enabledStr = Environment.GetEnvironmentVariable("AZURE_VISION_ENABLED");
        if (bool.TryParse(enabledStr, out bool enabled))
        {
            options.AzureVision.Enabled = enabled;
        }

        var language = Environment.GetEnvironmentVariable("AZURE_VISION_LANGUAGE");
        if (!string.IsNullOrEmpty(language))
        {
            options.AzureVision.Language = language;
        }

        var timeoutStr = Environment.GetEnvironmentVariable("AZURE_VISION_TIMEOUT_SECONDS");
        if (int.TryParse(timeoutStr, out int timeout))
        {
            options.AzureVision.TimeoutSeconds = timeout;
        }

        var confidenceStr = Environment.GetEnvironmentVariable("AZURE_VISION_MIN_CONFIDENCE");
        if (double.TryParse(confidenceStr, out double confidence))
        {
            options.AzureVision.MinConfidenceThreshold = confidence;
        }
    }

    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Alias for backward compatibility
        return AddLocalStorageServices(services, configuration);
    }
}