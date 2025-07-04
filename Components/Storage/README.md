# Storage Component

## Overview
The `Storage` component is a .NET class library responsible for handling local file system operations related to storing images and their associated data. It provides services for saving original images, generating and saving thumbnails, and managing the directory structure for these assets.

## Key Features
-   **Local File Storage:** Provides functionality to save byte arrays as files to a specified local directory.
-   **Thumbnail Generation:** Integrates with `ImageSharp` (or similar library) to create optimized thumbnail versions of images.
-   **Directory Management:** Ensures that necessary storage directories exist, creating them if they are missing.
-   **Configurable Storage Options:** Allows configuration of storage paths, maximum file sizes, and image optimization settings.

## Dependencies
This component depends on:
-   `Components/Domain` for domain entities like `BlobReference`.
-   `Microsoft.Extensions.Configuration` for configuration binding.
-   `Microsoft.Extensions.DependencyInjection` for service registration.
-   `SixLabors.ImageSharp` for image processing (resizing, encoding).

## Configuration
The `Storage` component is configured via `StorageOptions`, typically loaded from the "Storage" section of `appsettings.json` or environment variables.

**`StorageOptions` properties:**
-   `ScreenshotsDirectory`: (string) The relative or absolute path where original images will be stored.
-   `ThumbnailsDirectory`: (string) The relative or absolute path where generated thumbnails will be stored.
-   `CreateDirectoriesIfNotExist`: (bool) If true, the service will create the `ScreenshotsDirectory` and `ThumbnailsDirectory` if they do not exist.
-   `MaxFileSizeBytes`: (long) The maximum allowed size for an image file to be processed and stored.
-   `DefaultOptimization`: (ImageOptimizationSettings) Nested object for default image optimization settings:
    -   `ThumbnailWidth`: (int) Desired width for generated thumbnails.
    -   `ThumbnailHeight`: (int) Desired height for generated thumbnails.
    -   `ThumbnailFormat`: (string) Output format for thumbnails (e.g., "jpeg", "png").
    -   `ThumbnailQuality`: (int) Quality setting for lossy formats (e.g., JPEG, 1-100).

## Usage
To use the `Storage` component in another project, add a project reference to `Storage.csproj` and register its services in your `Program.cs` or startup class:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddLocalStorageServices(builder.Configuration);
```

Then, you can inject `ILocalStorageService` or `IScreenshotStorageService` into your services:

```csharp
public class MyService
{
    private readonly ILocalStorageService _localStorageService;

    public MyService(ILocalStorageService localStorageService)
    {}

    public async Task SaveImage(byte[] imageData, string fileName)
    {
        var uploadResult = await _localStorageService.SaveImageAsync(imageData, fileName);
        // Process uploadResult
    }
}
```

## Testing
The `Storage` component has dedicated unit tests in `Components/StorageTests/`. These tests cover file saving, thumbnail generation, and error handling.
