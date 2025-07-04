# Vision Component

## Overview
The `Vision` component is a .NET class library responsible for encapsulating all interactions with external Artificial Intelligence (AI) Vision APIs. Its primary purpose is to provide a standardized and abstracted interface for image analysis, allowing other parts of the application (e.g., `ImageAnalysisService`) to consume AI vision capabilities without direct knowledge of the underlying API specifics.

## Key Features
-   **Azure AI Vision API Integration:** Provides services for communicating with Azure AI Vision API 4.0 for image analysis.
-   **Configurable Features:** Allows specifying which AI vision features to request (e.g., read, tags, objects, people).
-   **Environment Variable Configuration:** Securely loads API endpoint and key from environment variables.
-   **Simulation Mode:** Includes an optional simulation mode for testing and development without making actual API calls.

## Dependencies
This component primarily depends on:
-   `Microsoft.Extensions.Configuration` for configuration binding.
-   `Microsoft.Extensions.DependencyInjection` for service registration.
-   `System.Net.Http` for making HTTP requests to the AI Vision API.

## Configuration
The `Vision` component is configured via `AzureVisionOptions`, which are typically loaded from application configuration (e.g., `appsettings.json`) and can be overridden by environment variables.

**`AzureVisionOptions` properties:**
-   `Enabled`: (bool) Enables or disables the Azure Vision API calls.
-   `Simulate`: (bool) If true, the service will return dummy analysis results without calling the actual API.
-   `Endpoint`: (string) The base URL for your Azure Vision resource. **(Loaded from `AZURE_VISION__ENDPOINT` environment variable)**
-   `ApiKey`: (string) Your Azure Vision API key. **(Loaded from `AZURE_VISION__APIKEY` environment variable)**
-   `TimeoutSeconds`: (int) Timeout for API calls in seconds.
-   `Language`: (string) The language for analysis results (e.g., "en").
-   `GenderNeutralCaption`: (bool) If true, captions will be gender-neutral.
-   `Features`: (List<string>) A list of features to request from the API (e.g., "read", "tags", "objects", "people").
-   `MinConfidenceThreshold`: (double) Minimum confidence score for including tags/objects in results.

**Environment Variable Configuration:**
Sensitive information like `Endpoint` and `ApiKey` should be provided via environment variables to avoid hardcoding.
-   `AZURE_VISION__ENDPOINT`
-   `AZURE_VISION__APIKEY`

Refer to the main `README.md` for detailed instructions on setting these environment variables.

## Usage
To use the `Vision` component in another project, add a project reference to `Vision.csproj` and register its services in your `Program.cs` or startup class:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddVisionServices(builder.Configuration);
```

Then, you can inject `AzureVisionHttpService` into your services:

```csharp
public class MyService
{
    private readonly AzureVisionHttpService _visionService;

    public MyService(AzureVisionHttpService visionService)
    {
        _visionService = visionService;
    }

    public async Task AnalyzeImage(byte[] imageData)
    {
        var analysisResult = await _visionService.AnalyzeImageAsync(imageData);
        // Process analysisResult
    }
}
```

## Testing
This component has dedicated unit tests in `Components/VisionTests/`, ensuring proper API interaction and response parsing, especially in simulation mode.
