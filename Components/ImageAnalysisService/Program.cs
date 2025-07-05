using DotNetEnv;
using ImageAnalysisService;
using Storage.Extensions;
using Vision.Configuration;
using Vision.Extensions;
using ViewerComponent.Extensions;

// Function to find .env file in parent directories
static string? FindEnvFile(string startDirectory)
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

// Load .env file
var envFilePath = FindEnvFile(Directory.GetCurrentDirectory());
if (envFilePath != null)
{
    Env.Load(envFilePath);
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Add Windows Service support
builder.Services.AddWindowsService();

// Add background services
builder.Services.AddSingleton<ProcessingChannel>();
builder.Services.AddOptions<FolderMonitorOptions>().Bind(builder.Configuration.GetSection(nameof(FolderMonitorOptions)));
builder.Services.AddLocalStorageServices(builder.Configuration);
builder.Services.AddVisionServices(builder.Configuration);
builder.Services.AddOptions<AzureVisionOptions>().Bind(builder.Configuration);
builder.Services.AddHostedService<FolderMonitorWorker>();
builder.Services.AddHostedService<ImageProcessorWorker>();

// Add ViewerComponent services
builder.Services.AddViewerComponent(builder.Configuration);

var app = builder.Build();

// Configure URLs from ViewerOptions
var viewerOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ViewerComponent.Configuration.ViewerOptions>>().Value;
app.Urls.Add($"http://{viewerOptions.Host}:{viewerOptions.Port}");

// Configure HTTP pipeline
app.UseRouting();

// Serve ViewerComponent static files
// First try the published location (wwwroot next to executable)
var viewerComponentPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");

// If not found, try the development location
if (!Directory.Exists(viewerComponentPath))
{
    viewerComponentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ViewerComponent", "wwwroot"));
}

if (Directory.Exists(viewerComponentPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(viewerComponentPath),
        RequestPath = ""
    });
    
    app.MapControllers();

    // Fallback to serve index.html for SPA routing
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(viewerComponentPath)
    });
}
else
{
    app.MapControllers();
    
    // If no wwwroot found, just serve a simple message
    app.MapGet("/", () => "ScreenshotAI is running, but web UI files are missing. Please check the wwwroot directory.");
}

app.Run();
