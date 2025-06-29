using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Storage.Extensions;

namespace LocalStorageTests;

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

        // Add local storage services
        services.AddLocalStorageServices(configuration);

        return services.BuildServiceProvider();
    }

    public static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }
}