# Domain Component

## Responsibility
- Define core business entities
- Enforce business rules and invariants
- Provide factory methods for entity creation

## Public Interface
- Screenshot entity with business methods
- Value objects for complex types
- Domain enums and constants

## Dependencies
- None (pure domain layer)

## Usage Example
```csharp
var screenshot = Screenshot.CreateFromClipboard("Screenshot_001", "blob123");
screenshot.MarkAsProcessed();
```

## Architecture

### Entities
- **Screenshot**: Core business entity representing a screenshot with its metadata, status, and business operations

### Value Objects
- **BlobReference**: Immutable value object representing a reference to a blob storage location

### Enums
- **ScreenshotSource**: Defines the source of the screenshot (Clipboard, FileUpload, DragDrop, API)
- **ScreenshotStatus**: Defines the processing status (Processing, Ready, Failed, Deleted)

## Business Rules

### Screenshot Entity
1. **Creation**: Screenshots can only be created through factory methods for different sources
2. **Display Name**: Must not be null or empty; can be updated unless screenshot is deleted
3. **Status Transitions**: 
   - New screenshots start in `Processing` status
   - Can transition to `Ready` when processing completes
   - Can transition to `Failed` with a reason when processing fails
   - Can transition to `Deleted` at any time
   - Deleted screenshots cannot be modified
4. **AI Analysis**: Can only be added to screenshots that are not deleted or failed

### BlobReference Value Object
1. **Immutability**: Once created, values cannot be changed
2. **Validation**: Container name, blob name, and URI must be provided and valid
3. **Equality**: Two BlobReference objects are equal if all properties match

## API Reference

### Screenshot Entity

#### Factory Methods
```csharp
public static Screenshot CreateFromClipboard(string displayName, string blobName)
public static Screenshot CreateFromUpload(string displayName, string blobName)
public static Screenshot CreateFromDragDrop(string displayName, string blobName)
public static Screenshot CreateFromAPI(string displayName, string blobName)
```

#### Business Methods
```csharp
public void UpdateDisplayName(string newName)
public void MarkAsProcessed()
public void MarkAsFailed(string reason)
public void MarkAsDeleted()
public void AddAIAnalysis(string extractedText, List<string> tags)
```

#### Properties
```csharp
public Guid Id { get; }
public string DisplayName { get; }
public string BlobName { get; }
public DateTime CreatedAt { get; }
public ScreenshotSource Source { get; }
public ScreenshotStatus Status { get; }
public string? ExtractedText { get; }
public List<string> Tags { get; }
public string? FailureReason { get; }

// Computed Properties
public bool HasAIAnalysis { get; }
public bool IsProcessing { get; }
public bool IsReady { get; }
public bool IsFailed { get; }
public bool IsDeleted { get; }
```

### BlobReference Value Object

#### Constructor
```csharp
public BlobReference(string containerName, string blobName, Uri fullUri)
```

#### Properties
```csharp
public string ContainerName { get; }
public string BlobName { get; }
public Uri FullUri { get; }
```

## Testing
- Unit tests for all business rules
- Property-based testing for invariants
- No external dependencies to mock

### Test Coverage
- ✅ Screenshot entity creation and validation
- ✅ Screenshot business logic and state transitions
- ✅ BlobReference value object equality and validation
- ✅ Enum definitions and string conversions
- ✅ Error handling and edge cases

## File Structure
```
Components/Domain/
├── README.md
├── src/
│   ├── Entities/
│   │   └── Screenshot.cs
│   ├── ValueObjects/
│   │   └── BlobReference.cs
│   ├── Enums/
│   │   ├── ScreenshotSource.cs
│   │   └── ScreenshotStatus.cs
│   └── Interfaces/
├── tests/
│   └── DomainTests/
│       ├── ScreenshotTests.cs
│       ├── BlobReferenceTests.cs
│       └── EnumTests.cs
└── docs/
    └── domain-model.md
```

## Version History
- **v1.0**: Initial implementation with Screenshot entity, BlobReference value object, and domain enums
- Complete test coverage for all domain logic
- Pure domain layer with no external dependencies