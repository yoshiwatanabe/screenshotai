# ScreenshotAI 📸🤖

[![Build Status](https://github.com/yoshiwatanabe/screenshotai/workflows/Build%20and%20Release/badge.svg)](https://github.com/yoshiwatanabe/screenshotai/actions)
[![Tests](https://github.com/yoshiwatanabe/screenshotai/workflows/Continuous%20Integration/badge.svg)](https://github.com/yoshiwatanabe/screenshotai/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Automated screenshot analysis with Azure AI Vision and real-time web visualization**

ScreenshotAI is a powerful .NET application that automatically monitors directories for new images, analyzes them using Azure AI Vision, and provides a beautiful web interface to view the results in real-time.

## ✨ Features

- 🔄 **Automated Processing**: Drop images in a folder and get instant AI analysis
- 🧠 **Azure AI Vision**: Advanced image analysis including objects, text, faces, and more
- 🌐 **Web Interface**: Beautiful, responsive web UI for viewing results
- 📊 **Real-time Updates**: See new analysis results as they're processed
- 🖥️ **Cross-Platform**: Windows, Linux, and macOS support
- 🔒 **Self-Contained**: No .NET runtime installation required
- 🎯 **REST API**: Programmatic access to images and analysis results

## 🚀 Quick Start

### Option 1: Download Pre-built Release (Recommended)

1. **Download** the latest release for your platform:
   - [📥 Windows](https://github.com/yoshiwatanabe/screenshotai/releases/latest/download/screenshotai-Windows.zip)
   - [📥 Linux](https://github.com/yoshiwatanabe/screenshotai/releases/latest/download/screenshotai-Linux.tar.gz)
   - [📥 macOS Intel](https://github.com/yoshiwatanabe/screenshotai/releases/latest/download/screenshotai-macOS-Intel.tar.gz)
   - [📥 macOS ARM64](https://github.com/yoshiwatanabe/screenshotai/releases/latest/download/screenshotai-macOS-ARM64.tar.gz)

2. **Extract** the downloaded file to your preferred location

3. **Set up Azure Vision API** (see [Azure Setup](#azure-vision-api-setup) below)

4. **Run** the application:
   ```bash
   # Windows
   ./ImageAnalysisService.exe
   
   # Linux/macOS  
   ./ImageAnalysisService
   ```

5. **Open** your browser to `http://localhost:8080`

6. **Drop images** in the `_watch` folder and see them analyzed automatically!

### Option 2: Build from Source

**Prerequisites:**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

```bash
# Clone the repository
git clone https://github.com/yoshiwatanabe/screenshotai.git
cd screenshotai

# Build and run
dotnet run --project Components/ImageAnalysisService
```

## 🔧 Azure Vision API Setup

ScreenshotAI requires Azure AI Vision for image analysis. Here's how to set it up:

### 1. Create Azure Vision Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click "Create a resource" → "AI + Machine Learning" → "Computer Vision"
3. Fill in the required information:
   - **Subscription**: Your Azure subscription
   - **Resource Group**: Create new or use existing
   - **Region**: Choose a region near you (e.g., East US 2)
   - **Name**: Choose a unique name (e.g., `my-screenshot-vision`)
   - **Pricing Tier**: Choose based on your needs (F0 is free tier)
4. Click "Review + Create" → "Create"

### 2. Get API Credentials

1. Navigate to your created Computer Vision resource
2. Go to "Keys and Endpoint" in the left sidebar
3. Copy the **Endpoint** and **Key 1**

### 3. Configure Environment Variables

Set the following environment variables with your Azure credentials:

**Windows (Command Prompt):**
```cmd
set AzureVision__Endpoint=https://your-resource.cognitiveservices.azure.com/
set AzureVision__ApiKey=your-api-key-here
```

**Windows (PowerShell):**
```powershell
$env:AzureVision__Endpoint="https://your-resource.cognitiveservices.azure.com/"
$env:AzureVision__ApiKey="your-api-key-here"
```

**Linux/macOS:**
```bash
export AzureVision__Endpoint="https://your-resource.cognitiveservices.azure.com/"
export AzureVision__ApiKey="your-api-key-here"
```

**Or create a `.env` file** in the project root:
```env
AzureVision__Endpoint=https://your-resource.cognitiveservices.azure.com/
AzureVision__ApiKey=your-api-key-here
```

## 🖥️ Usage

### Basic Workflow

1. **Start the service**: Run the executable or `dotnet run`
2. **Access web interface**: Open `http://localhost:8080` in your browser
3. **Add images**: Drop image files into the `_watch` folder
4. **View results**: Images appear in the web interface with AI analysis after processing
5. **Explore analysis**: Click on any image to see detailed AI analysis results

### Supported Image Formats

- PNG
- JPEG/JPG
- BMP
- GIF
- TIFF

### Directory Structure

```
screenshotai/
├── _watch/          # Drop images here for processing
├── _output/         # Processed images and analysis results
│   ├── thumbnails/  # Generated thumbnails
│   ├── *.png        # Processed images
│   └── *.json       # AI analysis results
└── ImageAnalysisService # The main executable
```

## 🌐 Web Interface

The web interface provides:

- **Grid View**: Thumbnail view of all processed images
- **Detail View**: Full-size image with formatted AI analysis
- **Real-time Updates**: Automatically refreshes when new images are processed
- **Search & Filter**: Find specific images quickly
- **Responsive Design**: Works on desktop and mobile

### AI Analysis Display

The interface shows rich analysis data including:

- 🏷️ **Tags**: Descriptive tags with confidence scores
- 🎯 **Objects**: Detected objects with bounding boxes
- 👤 **Faces**: Face detection and attributes
- 📝 **Text**: Extracted text (OCR)
- 🎨 **Colors**: Dominant colors and color analysis
- 📊 **Categories**: Image classification

## 🔌 API Endpoints

ScreenshotAI provides REST API endpoints for programmatic access:

- `GET /api/files` - List all processed images with metadata
- `GET /api/image/{filename}` - Retrieve specific image file
- `GET /api/analysis/{filename}` - Get AI analysis for an image
- `GET /api/status` - Service status and statistics

### Example API Usage

```bash
# Get all files
curl http://localhost:8080/api/files

# Get specific image
curl http://localhost:8080/api/image/screenshot.png

# Get analysis for an image
curl http://localhost:8080/api/analysis/screenshot.png
```

## ⚙️ Configuration

### Basic Configuration

The application can be configured via `appsettings.json`:

```json
{
  "Viewer": {
    "Port": 8080,
    "Host": "0.0.0.0",
    "OutputDirectory": "_output"
  },
  "FolderMonitorOptions": {
    "Path": "_watch"
  },
  "Storage": {
    "ScreenshotsDirectory": "_output",
    "ThumbnailsDirectory": "_output/thumbnails"
  }
}
```

### Advanced Configuration

For production environments, you can customize:

- **Network Settings**: Change host/port for the web interface
- **Directory Paths**: Configure input and output directories
- **Storage Options**: Adjust image optimization settings
- **Logging**: Configure log levels and outputs

## 🧪 Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Components/DomainTests
dotnet test Components/StorageTests
dotnet test Components/VisionTests
dotnet test Components/ImageAnalysisServiceTests
dotnet test Components/ViewerComponentTests
```

### Project Structure

The project follows a clean architecture with separate components:

- **`ImageAnalysisService`**: Main application host with web server
- **`ViewerComponent`**: Web interface for viewing results
- **`Vision`**: Azure AI Vision integration
- **`Storage`**: File system operations and optimization
- **`Domain`**: Core domain entities and value objects

Each component has its own `README.md` with detailed documentation.

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: Check component READMEs for detailed information
- **Issues**: [Report bugs](https://github.com/yoshiwatanabe/screenshotai/issues/new?template=bug_report.md) or [request features](https://github.com/yoshiwatanabe/screenshotai/issues/new?template=feature_request.md)
- **Discussions**: Join our [GitHub Discussions](https://github.com/yoshiwatanabe/screenshotai/discussions)

## 🔗 Links

- [Azure AI Vision Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/)
- [.NET 8.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Project Releases](https://github.com/yoshiwatanabe/screenshotai/releases)

---

**Made with ❤️ and powered by Azure AI Vision**