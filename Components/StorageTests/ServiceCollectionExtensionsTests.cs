using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Storage.Extensions;
using Storage.Services;
using Xunit;

namespace StorageTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStorageServices_ValidConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddStorageServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var storageService = serviceProvider.GetService<IBlobStorageService>();

        Assert.NotNull(storageService);
        Assert.IsType<AzureBlobStorageService>(storageService);
    }

    [Fact]
    public void AddStorageServices_RegistersRequiredServices()
    {
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddStorageServices(configuration);

        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IBlobStorageService));
        
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(typeof(AzureBlobStorageService), serviceDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:ConnectionString"] = "UseDevelopmentStorage=true",
            ["Storage:ContainerName"] = "test-screenshots",
            ["Storage:ThumbnailContainer"] = "test-thumbnails"
        });
        
        return configurationBuilder.Build();
    }
}