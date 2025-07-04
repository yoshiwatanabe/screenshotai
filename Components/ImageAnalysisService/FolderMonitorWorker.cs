
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageAnalysisService
{
    public class FolderMonitorWorker : BackgroundService
    {
        private readonly ILogger<FolderMonitorWorker> _logger;
        private readonly string _watchPath;
        private readonly ProcessingChannel _channel;
        private FileSystemWatcher? _watcher;

        public FolderMonitorWorker(ILogger<FolderMonitorWorker> logger, IOptions<FolderMonitorOptions> options, ProcessingChannel channel)
        {
            _logger = logger;
            _watchPath = options.Value.Path!;
            _channel = channel;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Folder Monitor Service starting.");

            if (!Directory.Exists(_watchPath))
            {
                _logger.LogWarning($"Watch path '{_watchPath}' not found. Creating it.");
                Directory.CreateDirectory(_watchPath);
            }

            _watcher = new FileSystemWatcher(_watchPath)
            {
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.png", // Initially watching for PNG files, can be configured
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;

            _logger.LogInformation($"Watching for new files in: {_watchPath}");

            return Task.CompletedTask;
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"New file detected: {e.FullPath}");
            await _channel.Writer.WriteAsync(e.FullPath);
        }

        public override void Dispose()
        {
            _watcher?.Dispose();
            base.Dispose();
        }
    }

    public class FolderMonitorOptions
    {
        public string? Path { get; set; }
    }
}
