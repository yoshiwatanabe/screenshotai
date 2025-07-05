using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ViewerComponent.Configuration;
using ViewerComponent.Models;

namespace ViewerComponent.Services;

public class ViewerService : IViewerService
{
    private readonly ViewerOptions _options;
    private readonly ILogger<ViewerService> _logger;

    public ViewerService(IOptions<ViewerOptions> options, ILogger<ViewerService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IEnumerable<ImageFileInfo>> GetImageFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var outputDirectory = Path.GetFullPath(_options.OutputDirectory);

            if (!Directory.Exists(outputDirectory))
            {
                _logger.LogWarning("Output directory does not exist: {Directory}", outputDirectory);
                return Task.FromResult(Enumerable.Empty<ImageFileInfo>());
            }

            var imageFiles = Directory.GetFiles(outputDirectory, "*.png", SearchOption.AllDirectories)
                .Select(imagePath => CreateImageFileInfo(imagePath, outputDirectory))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            _logger.LogDebug("Found {Count} image files in {Directory}", imageFiles.Count, outputDirectory);
            return Task.FromResult<IEnumerable<ImageFileInfo>>(imageFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image files from directory: {Directory}", _options.OutputDirectory);
            return Task.FromResult(Enumerable.Empty<ImageFileInfo>());
        }
    }

    public async Task<byte[]?> GetImageAsync(string filename, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedFilename = Path.GetFileName(filename);
            var imagePath = Path.Combine(_options.OutputDirectory, sanitizedFilename);

            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Image file not found: {FilePath}", imagePath);
                return null;
            }

            return await File.ReadAllBytesAsync(imagePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading image file: {Filename}", filename);
            return null;
        }
    }

    public async Task<string?> GetAnalysisAsync(string filename, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedFilename = Path.GetFileName(filename);
            var jsonFilename = Path.ChangeExtension(sanitizedFilename, ".json");
            var jsonPath = Path.Combine(_options.OutputDirectory, jsonFilename);

            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("Analysis file not found: {FilePath}", jsonPath);
                return null;
            }

            return await File.ReadAllTextAsync(jsonPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading analysis file: {Filename}", filename);
            return null;
        }
    }

    public async Task<ViewerStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var imageFiles = await GetImageFilesAsync(cancellationToken);
            var imageFilesList = imageFiles.ToList();

            return new ViewerStatus
            {
                TotalImages = imageFilesList.Count,
                ImagesWithAnalysis = imageFilesList.Count(x => x.HasAnalysis),
                LastUpdated = DateTime.UtcNow,
                OutputDirectory = _options.OutputDirectory
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting viewer status");
            return new ViewerStatus
            {
                LastUpdated = DateTime.UtcNow,
                OutputDirectory = _options.OutputDirectory
            };
        }
    }

    private ImageFileInfo CreateImageFileInfo(string imagePath, string baseDirectory)
    {
        var fileInfo = new FileInfo(imagePath);
        var relativePath = Path.GetRelativePath(baseDirectory, imagePath);
        var jsonPath = Path.ChangeExtension(imagePath, ".json");

        return new ImageFileInfo
        {
            FileName = fileInfo.Name,
            ImagePath = relativePath,
            JsonPath = File.Exists(jsonPath) ? Path.ChangeExtension(relativePath, ".json") : null,
            CreatedAt = fileInfo.CreationTime,
            FileSize = fileInfo.Length
        };
    }
}