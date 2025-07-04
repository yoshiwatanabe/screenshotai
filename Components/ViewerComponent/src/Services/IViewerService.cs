using ViewerComponent.Models;

namespace ViewerComponent.Services;

public interface IViewerService
{
    Task<IEnumerable<ImageFileInfo>> GetImageFilesAsync(CancellationToken cancellationToken = default);
    Task<byte[]?> GetImageAsync(string filename, CancellationToken cancellationToken = default);
    Task<string?> GetAnalysisAsync(string filename, CancellationToken cancellationToken = default);
    Task<ViewerStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}