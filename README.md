# screenshotai

## Project Overview
This project, `screenshotai`, is a .NET application designed to monitor a specified directory for new image files, analyze them using Azure AI Vision, and store the analysis results alongside the images. The primary goal is to provide an automated image analysis service.

## Architecture
The application follows a modular and component-based architecture, primarily focusing on a background service model. Key architectural principles include:
-   **Decoupling:** Components are designed to be independent and communicate through well-defined interfaces.
-   **Extensibility:** The design allows for easy integration of new AI vision providers or storage mechanisms.
-   **Observability:** Logging is integrated to provide insights into the service's operation.

The core workflow involves:
1.  **Folder Monitoring:** A dedicated service monitors a local directory for new image files.
2.  **Image Processing:** Detected images are queued for asynchronous processing.
3.  **AI Analysis:** Images are sent to an AI Vision service (currently Azure AI Vision) for analysis.
4.  **Storage:** Original images, generated thumbnails, and AI analysis results are stored locally.

## Core Components
The project is structured into several key components, each with a specific responsibility:

-   **`ImageAnalysisService`**: The main application host. It orchestrates folder monitoring, image processing, and integration with `Vision` and `Storage` components.
-   **`Vision`**: Encapsulates all interactions with external AI Vision APIs (e.g., Azure AI Vision). It handles API calls, request/response serialization, and provides a clean interface for image analysis.
-   **`Storage`**: Manages local file system operations, including saving original images, generating thumbnails, and storing analysis metadata.
-   **`Domain`**: Defines the core domain entities, value objects, and enumerations (e.g., `Screenshot`, `BlobReference`) that are shared across different components.
-   **`DomainTests`**: Contains unit tests for the `Domain` component.
-   **`LocalStorageTests`**: Contains unit and integration tests for the `Storage` component's local file storage functionality.

Each component has its own `README.md` file providing more detailed information.

## Configuration

### Azure Vision API

The Image Analysis Service requires Azure Vision API credentials. These should be provided via environment variables to avoid hardcoding sensitive information.

*   `AzureVision__Endpoint`: The endpoint URL for your Azure Vision resource.
*   `AzureVision__ApiKey`: Your Azure Vision API key.

**Example (Linux/macOS):**

```bash
export AzureVision__Endpoint="YOUR_AZURE_VISION_ENDPOINT"
export AzureVision__ApiKey="YOUR_AZURE_VISION_API_KEY"
```

**Example (Windows Command Prompt):**

```cmd
set AzureVision__Endpoint="YOUR_AZURE_VISION_ENDPOINT"
set AzureVision__ApiKey="YOUR_AZURE_VISION_API_KEY"
```

**Example (Windows PowerShell):**

```powershell
$env:AzureVision__Endpoint="YOUR_AZURE_VISION_ENDPOINT"
$env:AzureVision__ApiKey="YOUR_AZURE_VISION_API_KEY"
```

Remember to replace `YOUR_AZURE_VISION_ENDPOINT` and `YOUR_AZURE_VISION_API_KEY` with your actual credentials.