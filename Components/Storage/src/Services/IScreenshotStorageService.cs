using Storage.Models;

namespace Storage.Services;

/// <summary>
/// Interface for screenshot storage services
/// </summary>
public interface IScreenshotStorageService : ILocalStorageService
{
    // This interface extends ILocalStorageService for now
    // Can be expanded with additional screenshot-specific methods if needed
}