# Screenshot Manager - System Architecture

## Architecture Overview

The Screenshot Manager follows a **layered architecture** with clear separation of concerns, designed to evolve from simple file management to sophisticated AI-powered analysis across the three development stages.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Layer                            │
│  Web Browser (HTML5, JavaScript, Bootstrap 5)              │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTPS
┌─────────────────────┴───────────────────────────────────────┐
│                Web Application Layer                        │
│  ASP.NET Core MVC (.NET 8) - Azure App Service             │
├─────────────────────────────────────────────────────────────┤
│  • Controllers (Upload, Gallery, Search)                   │
│  • Views (Razor Pages)                                     │
│  • ViewModels                                              │
│  • Middleware (Auth, Error Handling)                       │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                  Service Layer                              │
│  Business Logic & Integration Services                      │
├─────────────────────────────────────────────────────────────┤
│  • IBlobStorageService                                      │
│  • IImageAnalysisService (Walk+)                           │
│  • ISearchService (Walk+)                                  │
│  • IMetadataService                                        │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                   Data Layer                               │
├─────────────────────────────────────────────────────────────┤
│  Azure Blob Storage     │  Azure SQL Database (Walk+)      │
│  • Screenshot files     │  • Metadata                      │
│  • Thumbnails          │  • AI Analysis Results           │
│                        │  • Search Indexes                │
└─────────────────────────┴───────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│                External Services                            │
├─────────────────────────────────────────────────────────────┤
│  Azure AI Foundry (Walk+)  │  GitHub Actions               │
│  • Computer Vision API     │  • CI/CD Pipeline             │
│  • OCR Services           │  • Automated Deployment       │
│  • Custom Models (Run)    │                               │
└─────────────────────────────────────────────────────────────┘
```

## Component Architecture

### 1. Domain Layer
**Location**: `src/ScreenshotManager.Domain/`

```csharp
// Core entities and value objects
public class Screenshot
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; }
    public string BlobName { get; set; }
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public ScreenshotMetadata Metadata { get; set; }
}

public class ScreenshotMetadata
{
    public string ExtractedText { get; set; }  // Walk+
    public List<string> AiTags { get; set; }   // Walk+
    public string Description { get; set; }    // Walk+
    public ApplicationType DetectedApp { get; set; } // Walk+
}
```

### 2. Application Services Layer
**Location**: `src/ScreenshotManager.Application/`

#### Storage Service
```csharp
public interface IBlobStorageService
{
    Task<UploadResult> UploadScreenshotAsync(IFormFile file, CancellationToken cancellationToken);
    Task<Stream> GetScreenshotAsync(string blobName, CancellationToken cancellationToken);
    Task<Uri> GetThumbnailUriAsync(string blobName);
    Task<bool> DeleteScreenshotAsync(string blobName, CancellationToken cancellationToken);
}
```

#### AI Analysis Service (Walk+)
```csharp
public interface IImageAnalysisService
{
    Task<AnalysisResult> AnalyzeScreenshotAsync(Stream imageStream, CancellationToken cancellationToken);
    Task<List<string>> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken);
    Task<List<string>> GenerateTagsAsync(Stream imageStream, CancellationToken cancellationToken);
}
```

### 3. Infrastructure Layer
**Location**: `src/ScreenshotManager.Infrastructure/`

#### Configuration
```csharp
public class AzureStorageOptions
{
    public string ConnectionString { get; set; }
    public string ContainerName { get; set; } = "screenshots";
    public string ThumbnailContainer { get; set; } = "thumbnails";
}

public class AzureAIOptions
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string Region { get; set; } = "eastus";
}
```

## Data Architecture

### Stage Evolution

#### Crawl Stage
```
Azure Blob Storage
├── screenshots/          # Original uploaded files
├── thumbnails/          # Auto-generated thumbnails
└── metadata.json        # Simple file metadata
```

#### Walk Stage
```
Azure SQL Database
├── Screenshots          # Core file information
├── AnalysisResults      # AI analysis metadata
├── ExtractedText        # OCR results
└── Tags                 # AI-generated tags

Azure Blob Storage (unchanged)
```

#### Run Stage
```
Azure SQL Database
├── [Previous tables]
├── Projects             # Organized collections
├── SharedLinks          # Collaboration features
├── UserAnnotations      # Comments and notes
└── SearchIndex          # Optimized search data

Azure Cognitive Search (optional)
├── Full-text indexes
└── Semantic search capabilities
```

## Security Architecture

### Authentication & Authorization
```
┌─────────────────────┐    ┌─────────────────────┐
│   Azure AD B2C      │    │  App Service       │
│   (Future: Run)     │    │  Authentication    │
│                     │    │  (Basic: Crawl)    │
└─────────────────────┘    └─────────────────────┘
            │                        │
            └────────────┬───────────┘
                         │
            ┌─────────────┴───────────┐
            │    Authorization       │
            │    Middleware          │
            │  • Role-based access   │
            │  • Resource ownership  │
            └─────────────────────────┘
```

### Data Security
- **Encryption at Rest**: Azure Storage encryption
- **Encryption in Transit**: HTTPS/TLS
- **Access Control**: Azure RBAC and SAS tokens
- **API Security**: Rate limiting and input validation

## Deployment Architecture

### Azure Resources
```yaml
# Crawl Stage
- Resource Group: screenshot-manager-rg
- App Service Plan: screenshot-manager-plan (Free/Basic)
- App Service: screenshot-manager-app
- Storage Account: screenshotstorage
- Application Insights: screenshot-manager-insights

# Walk Stage (Additional)
- Azure SQL Database: screenshot-manager-db
- Azure AI Services: screenshot-manager-ai

# Run Stage (Additional)
- Azure Cognitive Search: screenshot-manager-search
- Azure Key Vault: screenshot-manager-vault
- Azure CDN: For global content delivery
```

### CI/CD Pipeline
```yaml
GitHub Actions Workflow:
1. Source Code → GitHub Repository
2. Build → .NET 8 Application
3. Test → Unit & Integration Tests
4. Deploy → Azure App Service
5. Post-Deploy → Health Checks
```

## Performance Architecture

### Caching Strategy
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Browser       │    │   App Service   │    │   Azure Storage │
│   Cache         │    │   Memory Cache  │    │   CDN (Run)     │
│                 │    │                 │    │                 │
│ • Thumbnails    │    │ • Metadata      │    │ • Static files  │
│ • Static assets │    │ • Search results│    │ • Thumbnails    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Scalability Considerations
- **Horizontal Scaling**: App Service auto-scaling
- **Database Scaling**: Azure SQL elastic pools (Walk+)
- **Storage Scaling**: Azure Blob Storage auto-scales
- **CDN**: Global distribution for static content (Run)

## Monitoring & Observability

### Application Insights Integration
```csharp
services.AddApplicationInsightsTelemetry();

// Custom telemetry
public class UploadTelemetry
{
    public void TrackUploadEvent(string fileName, long fileSize, TimeSpan duration);
    public void TrackAIAnalysis(string operation, TimeSpan duration, bool success);
}
```

### Key Metrics
- **Upload Performance**: File size vs upload time
- **AI Processing**: Analysis duration and accuracy
- **User Engagement**: Feature usage patterns
- **System Health**: Error rates, response times

## Technology Decisions

### Framework Choices
| Component | Technology | Rationale |
|-----------|------------|-----------|
| Web Framework | ASP.NET Core MVC | Microsoft stack consistency, performance |
| UI Framework | Bootstrap 5 | Rapid development, responsive design |
| Database | Azure SQL | Relational data, strong consistency |
| File Storage | Azure Blob Storage | Cost-effective, scalable |
| AI Services | Azure AI Foundry | Integrated ecosystem, latest models |

### Design Patterns
- **Repository Pattern**: Data access abstraction
- **Service Layer Pattern**: Business logic separation
- **Dependency Injection**: Loose coupling, testability
- **CQRS (Run stage)**: Read/write optimization for complex queries

---

**Last Updated**: June 17, 2025  
**Version**: 1.0  
**Architects**: Development Team  
**Next Review**: After technical design completion