# Domain Component

## Overview
The `Domain` component is a .NET class library that defines the core domain entities, value objects, and enumerations for the `screenshotai` application. It represents the fundamental business concepts and rules, independent of any specific application layer or infrastructure concerns.

## Key Features
-   **Core Entities:** Defines central entities such as `Screenshot`, representing the data model for captured or processed images.
-   **Value Objects:** Includes value objects like `BlobReference`, which encapsulate specific data patterns and their associated behaviors.
-   **Enumerations:** Provides enumerations like `ScreenshotSource` and `ScreenshotStatus` to define discrete sets of related values, ensuring type safety and clarity.
-   **Business Rules:** Encapsulates domain-specific business rules and invariants within the entities and value objects themselves.

## Dependencies
This component has no external project dependencies within the `screenshotai` solution, making it a foundational and highly reusable layer. It relies only on standard .NET libraries.

## Usage
Other components in the `screenshotai` solution, such as `ImageAnalysisService` and `Storage`, reference the `Domain` component to utilize its defined entities, value objects, and enumerations.

For example, to use the `Screenshot` entity:

```csharp
using Domain.Entities;
using Domain.Enums;

public class MyService
{
    public void ProcessScreenshot()
    {
        var screenshot = new Screenshot
        {
            Id = Guid.NewGuid(),
            FileName = "example.png",
            Source = ScreenshotSource.FileSystem,
            Status = ScreenshotStatus.New,
            Timestamp = DateTimeOffset.UtcNow
        };

        // ... further processing
    }
}
```

## Testing
The `Domain` component has dedicated unit tests in `Components/DomainTests/`, ensuring the correctness and integrity of its entities, value objects, and business rules.
