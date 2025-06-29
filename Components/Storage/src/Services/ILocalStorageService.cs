using Storage.Models;

namespace Storage.Services;

public interface ILocalStorageService
{
    Task<UploadResult> UploadFromClipboardAsync(
        byte[] imageData, 
        string fileName,
        CancellationToken cancellationToken = default);

    Task<OptimizedImageResult> OptimizeImageAsync(
        byte[] originalImage,
        ImageOptimizationSettings settings);

    Task<Stream> GetImageStreamAsync(string fileName, CancellationToken cancellationToken = default);

    Task<string> GetThumbnailPathAsync(string fileName);

    Task<string> GetImagePathAsync(string fileName);

    Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}