using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenshotManagerApp.Configuration;
using System.Windows;

namespace ScreenshotManagerApp;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Create host builder for dependency injection
            var host = CreateHostBuilder(args).Build();
            
            // Get the main application from DI container
            var app = host.Services.GetRequiredService<App>();
            
            // Run the WPF application
            app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start Screenshot Manager: {ex.Message}", 
                "Screenshot Manager - Startup Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure all application services
                services.ConfigureApplicationServices(context.Configuration);
            })
            .UseConsoleLifetime(); // Use console lifetime for background service hosting
}