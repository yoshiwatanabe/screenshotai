# ViewerComponent

## Overview
The ViewerComponent provides a lightweight web-based interface for viewing screenshot images alongside their Azure AI Vision analysis results. This component is designed to be integrated with the ImageAnalysisService to serve as a real-time viewer for processed screenshots.

## Architecture

### Design Principles
- **Decoupled**: Operates as a separate component with clean interfaces
- **Integrated**: Embeds within the ImageAnalysisService for seamless operation
- **Lightweight**: Minimal HTTP server with static web files
- **Real-time**: Automatically discovers new files as they're processed

### Component Structure
```
ViewerComponent/
├── README.md                     # This documentation
├── ViewerComponent.csproj        # Project file with dependencies
├── src/
│   ├── Configuration/
│   │   └── ViewerOptions.cs      # Configuration options
│   ├── Services/
│   │   ├── IViewerService.cs     # Interface for viewer operations
│   │   └── ViewerService.cs      # Core viewer service implementation
│   ├── Controllers/
│   │   └── ViewerController.cs   # HTTP endpoints for file operations
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration
└── wwwroot/                      # Static web files
    ├── index.html                # Main viewer interface
    ├── js/
    │   └── app.js                # JavaScript application logic
    └── css/
        └── styles.css            # Styling for the interface
```

## Integration Design

### HTTP Server Integration
The ViewerComponent integrates with the ImageAnalysisService by:
1. **Hosting**: Adds ASP.NET Core hosting to the existing background service
2. **Static Files**: Serves HTML, CSS, and JavaScript from wwwroot
3. **API Endpoints**: Provides REST endpoints for file operations
4. **Configuration**: Uses the same configuration system as other components

### API Endpoints
- `GET /` - Serves the main viewer interface
- `GET /api/files` - Returns list of all image/JSON file pairs
- `GET /api/image/{filename}` - Serves image files
- `GET /api/analysis/{filename}` - Serves JSON analysis results
- `GET /api/status` - Returns service status and statistics

### Configuration
```json
{
  "Viewer": {
    "Port": 8080,
    "Host": "localhost",
    "EnableCors": true,
    "StaticFilesPath": "wwwroot"
  }
}
```

## User Interface Design

### Main Features
1. **Grid View**: Displays thumbnails of all processed screenshots
2. **Detail View**: Shows full-size image with formatted analysis results
3. **Search & Filter**: Allows filtering by filename, date, or analysis content
4. **Real-time Updates**: Automatically refreshes when new files are processed
5. **Responsive Design**: Works on desktop and mobile devices

### Azure Vision Data Display
The interface presents Azure Vision analysis results in a structured format:
- **Object Detection**: Visual overlays on images with bounding boxes
- **Text Recognition**: Extracted text with confidence scores
- **Face Detection**: Face locations and attributes
- **Categories & Tags**: Detected categories and descriptive tags
- **Color Analysis**: Dominant colors and color schemes
- **Adult Content**: Safety classifications (if enabled)

### User Experience Flow
1. User starts the ImageAnalysisService (which includes ViewerComponent)
2. Service begins monitoring the specified directory
3. User opens browser to `http://localhost:8080`
4. Interface displays all existing processed screenshots
5. As new screenshots are processed, they appear automatically in the interface
6. User can click on any image to view detailed analysis results

## Technical Implementation

### Dependencies
- **ASP.NET Core**: Minimal hosting for HTTP server
- **Microsoft.Extensions.FileProviders**: For serving static files
- **Microsoft.Extensions.Hosting**: Integration with existing background service
- **Newtonsoft.Json**: JSON processing for analysis results

### File Discovery
The component automatically discovers image/JSON pairs by:
1. Scanning the configured output directory
2. Matching `.png` files with corresponding `.json` files
3. Providing metadata about file creation times and sizes
4. Monitoring for new files using the existing folder monitoring infrastructure

### Performance Considerations
- **Caching**: Analysis results are cached to avoid repeated file reads
- **Lazy Loading**: Images are loaded on-demand to improve initial page load
- **Compression**: Static files are served with compression when possible
- **Efficient Updates**: Only changed files trigger UI updates

## Development & Testing

### Running the Viewer
The ViewerComponent is automatically started when the ImageAnalysisService runs:
```bash
dotnet run --project Components/ImageAnalysisService
```

Then open a browser to `http://localhost:8080` to access the viewer.

### Testing
- Unit tests for ViewerService and file discovery logic
- Integration tests for HTTP endpoints
- End-to-end tests for the complete viewer workflow

### Development Notes
- The component follows the same patterns as other components in the project
- All configuration follows the existing configuration conventions
- Logging is integrated with the existing logging infrastructure
- Error handling follows the project's error handling patterns

## Future Enhancements
- **Export Features**: Export analysis results to various formats
- **Batch Operations**: Select multiple images for batch operations
- **Comparison View**: Side-by-side comparison of multiple images
- **Analysis History**: Track changes in analysis results over time
- **Custom Overlays**: User-defined overlays for specific analysis types