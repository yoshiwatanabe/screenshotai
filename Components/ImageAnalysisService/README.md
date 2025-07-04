# ImageAnalysisService Component

## Overview
The `ImageAnalysisService` is the main application component responsible for monitoring a designated folder for new image files, orchestrating their analysis using the `Vision` component, and storing the results using the `Storage` component. It now includes an integrated web server hosting the `ViewerComponent` for real-time visualization of analysis results.

## Key Features
-   **Folder Monitoring:** Continuously watches a specified directory for newly created image files.
-   **Image Processing Pipeline:** Decouples file detection from processing using a thread-safe channel.
-   **AI Image Analysis Integration:** Utilizes the `Vision` component to send images to Azure AI Vision for analysis (e.g., tags, objects, text).
-   **Analysis Result Storage:** Uses the `Storage` component to save processed images (including thumbnails) and their AI analysis results (as JSON sidecar files).
-   **Integrated Web Viewer:** Hosts the `ViewerComponent` providing a web-based interface for viewing processed images and their analysis results.
-   **HTTP API Endpoints:** Provides REST API access to images, analysis results, and service status.
-   **Robust Error Handling:** Includes mechanisms to log and handle errors during file monitoring, image processing, and API interactions.

## Dependencies
This component depends on:
-   `Components/Vision` for AI image analysis.
-   `Components/Storage` for local file storage operations.
-   `Components/Domain` for core domain entities like `Screenshot`.
-   `Components/ViewerComponent` for web-based visualization interface.
-   `DotNetEnv` for loading environment variables from `.env` files.
-   `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection` for host management, logging, configuration, and dependency injection.
-   ASP.NET Core for web hosting and HTTP API endpoints.

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
-   **`Viewer`**: This section configures the integrated web viewer:
    -   `Port`: (int) HTTP server port (default: 8080).
    -   `Host`: (string) HTTP server host (default: "localhost").
    -   `EnableCors`: (bool) Enable CORS for API access (default: true).
    -   `OutputDirectory`: (string) Directory path for served images and analysis files.

## Usage
To run the `ImageAnalysisService`:

```bash
dotnet run --project Components/ImageAnalysisService/ImageAnalysisService.csproj
```

The service will start and be available at:
- **Web Interface**: http://localhost:8080 (or configured port)
- **API Endpoints**:
  - `GET /api/files` - List all processed images with metadata
  - `GET /api/image/{filename}` - Retrieve specific image file
  - `GET /api/analysis/{filename}` - Retrieve analysis JSON for an image
  - `GET /api/status` - Service status and statistics

Ensure that the required environment variables (`AZURE_VISION__ENDPOINT`, `AZURE_VISION__APIKEY`) are set before running the application. Refer to the main `README.md` for detailed instructions.

## Testing
This component has dedicated unit tests in `Components/ImageAnalysisServiceTests/`.
