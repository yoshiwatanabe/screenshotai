using DotNetEnv;
using ImageAnalysisService;
using Storage.Extensions;

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

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<ProcessingChannel>();
builder.Services.AddOptions<FolderMonitorOptions>().Bind(builder.Configuration.GetSection(nameof(FolderMonitorOptions)));
builder.Services.AddLocalStorageServices(builder.Configuration);
builder.Services.AddHostedService<FolderMonitorWorker>();
builder.Services.AddHostedService<ImageProcessorWorker>();

var host = builder.Build();
host.Run();
