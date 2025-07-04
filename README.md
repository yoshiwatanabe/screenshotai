# screenshotai

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
