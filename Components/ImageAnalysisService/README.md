# ImageAnalysisService Component

## Overview
The `ImageAnalysisService` is the main application component responsible for monitoring a designated folder for new image files, orchestrating their analysis using the `Vision` component, and storing the results using the `Storage` component. It runs as a .NET Generic Host application, designed to operate as a background service.

## Key Features
-   **Folder Monitoring:** Continuously watches a specified directory for newly created image files.
-   **Image Processing Pipeline:** Decouples file detection from processing using a thread-safe channel.
-   **AI Image Analysis Integration:** Utilizes the `Vision` component to send images to Azure AI Vision for analysis (e.g., tags, objects, text).
-   **Analysis Result Storage:** Uses the `Storage` component to save processed images (including thumbnails) and their AI analysis results (as JSON sidecar files).
-   **Robust Error Handling:** Includes mechanisms to log and handle errors during file monitoring, image processing, and API interactions.

## Dependencies
This component depends on:
-   `Components/Vision` for AI image analysis.
-   `Components/Storage` for local file storage operations.
-   `Components/Domain` for core domain entities like `Screenshot`.
-   `DotNetEnv` for loading environment variables from `.env` files.
-   `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection` for host management, logging, configuration, and dependency injection.

## Configuration
The `ImageAnalysisService` is configured through `appsettings.json` and environment variables. Key configuration sections include:

-   **`Logging`**: Standard .NET logging configuration.
-   **`FolderMonitorOptions`**:
    -   `Path`: (string) The absolute path to the directory to monitor for new images.
-   **`Storage`**: This section configures the `Storage` component, including:
    -   `ScreenshotsDirectory`: Where original images are stored.
    -   `ThumbnailsDirectory`: Where generated thumbnails are stored.
    -   `CreateDirectoriesIfNotExist`: (bool) Whether to create output directories if they don't exist.
    -   `MaxFileSizeBytes`: (long) Maximum allowed file size for processing.
    -   `AzureVision`: This nested section configures the `Vision` component's `AzureVisionOptions`. Its `Endpoint` and `ApiKey` are primarily loaded from environment variables (`AZURE_VISION__ENDPOINT`, `AZURE_VISION__APIKEY`).

## Usage
To run the `ImageAnalysisService`:

```bash
dotnet run --project Components/ImageAnalysisService/ImageAnalysisService.csproj
```

Ensure that the required environment variables (`AZURE_VISION__ENDPOINT`, `AZURE_VISION__APIKEY`) are set before running the application. Refer to the main `README.md` for detailed instructions.

## Testing
This component has dedicated unit tests in `Components/ImageAnalysisServiceTests/`.
