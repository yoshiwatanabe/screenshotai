using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ViewerComponent.Configuration;
using ViewerComponent.Services;
using ViewerComponent.Extensions;

namespace ViewerComponentTests;

public class ConfigurationTests
{
    [Fact]
    public void ViewerOptions_DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new ViewerOptions();

        // Assert
        Assert.Equal(8080, options.Port);
        Assert.Equal("localhost", options.Host);
        Assert.True(options.EnableCors);
        Assert.Equal("wwwroot", options.StaticFilesPath);
        Assert.Equal("_output", options.OutputDirectory);
    }

    [Fact]
    public void ViewerOptions_SectionName_IsCorrect()
    {
        // Assert
        Assert.Equal("Viewer", ViewerOptions.SectionName);
    }

    [Fact]
    public void AddViewerComponent_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services for ViewerService
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Viewer:Port"] = "9090",
                ["Viewer:Host"] = "0.0.0.0",
                ["Viewer:EnableCors"] = "false"
            })
            .Build();

        // Act
        services.AddViewerComponent(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var viewerService = serviceProvider.GetService<IViewerService>();
        Assert.NotNull(viewerService);
        Assert.IsType<ViewerService>(viewerService);
    }

    [Fact]
    public void AddViewerComponent_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Viewer:Port"] = "9090",
                ["Viewer:Host"] = "0.0.0.0",
                ["Viewer:EnableCors"] = "false",
                ["Viewer:OutputDirectory"] = "/custom/output"
            })
            .Build();

        // Act
        services.AddViewerComponent(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ViewerOptions>>();
        Assert.NotNull(options);
        
        var viewerOptions = options.Value;
        Assert.Equal(9090, viewerOptions.Port);
        Assert.Equal("0.0.0.0", viewerOptions.Host);
        Assert.False(viewerOptions.EnableCors);
        Assert.Equal("/custom/output", viewerOptions.OutputDirectory);
    }

    [Fact]
    public void AddViewerComponent_RegistersControllers()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddViewerComponent(configuration);

        // Assert
        var serviceDescriptors = services.ToList();
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType.Name.Contains("Controller"));
    }

    [Fact]
    public void AddViewerComponent_RegistersCors()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddViewerComponent(configuration);

        // Assert
        var serviceDescriptors = services.ToList();
        Assert.Contains(serviceDescriptors, sd => sd.ServiceType.Name.Contains("Cors"));
    }

    [Fact]
    public void ViewerService_CanBeResolvedWithDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Viewer:OutputDirectory"] = Path.GetTempPath()
            })
            .Build();

        services.AddViewerComponent(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var viewerService = serviceProvider.GetRequiredService<IViewerService>();

        // Assert
        Assert.NotNull(viewerService);
        Assert.IsType<ViewerService>(viewerService);
    }

    [Fact]
    public void Configuration_MissingSection_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build(); // Empty configuration

        // Act
        services.AddViewerComponent(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ViewerOptions>>();
        var viewerOptions = options.Value;
        
        // Should use default values when configuration is missing
        Assert.Equal(8080, viewerOptions.Port);
        Assert.Equal("localhost", viewerOptions.Host);
        Assert.True(viewerOptions.EnableCors);
    }
}