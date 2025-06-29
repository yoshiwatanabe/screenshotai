using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Extensions;

namespace StorageIntegrationTests;

public static class TestConfiguration
{
    public static IServiceProvider CreateServiceProvider()
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });

        // Add storage services
        services.AddStorageServices(configuration);

        return services.BuildServiceProvider();
    }

    public static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TestConfiguration>(); // This will load from user secrets

        return builder.Build();
    }

    public static bool IsAzureStorageConfigured(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Storage") 
                            ?? configuration["Storage:ConnectionString"];
        
        return !string.IsNullOrEmpty(connectionString) && 
               connectionString != "PLACEHOLDER_FOR_USER_SECRETS";
    }
}