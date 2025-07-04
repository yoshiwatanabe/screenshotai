# LocalStorageTests Component

## Overview
The `LocalStorageTests` component is a .NET class library containing unit and integration tests specifically for the `LocalFileStorageService` within the `Storage` component. These tests verify the correct functionality of file saving, thumbnail generation, and directory management on the local file system.

## Key Features
-   **File System Interaction Testing:** Focuses on testing the interactions of `LocalFileStorageService` with the actual file system.
-   **Configuration Testing:** Ensures that `StorageOptions` are correctly applied and influence the behavior of the storage service.
-   **Error Handling Validation:** Verifies how the service handles various error conditions related to file operations.

## Dependencies
This component depends on:
-   `Components/Storage` (the project containing `LocalFileStorageService`).
-   `Microsoft.NET.Test.Sdk` for the test SDK.
-   `NUnit` for the testing framework.
-   `NUnit3TestAdapter` for running NUnit tests.
-   `Microsoft.Extensions.Configuration` for test configuration.
-   `Microsoft.Extensions.Options` for options pattern testing.

## Configuration for Tests
Tests in this project often rely on `appsettings.json` (e.g., `appsettings.json` within `LocalStorageTests` itself) to configure storage paths and other options for testing purposes. This allows tests to run against specific, isolated directories.

## Usage
These tests are typically run using a test runner integrated into an IDE (like Visual Studio Code with C# extensions) or from the command line.

To run tests from the command line:

```bash
dotnet test Components/LocalStorageTests/LocalStorageTests.csproj
```

## Testing Strategy
Tests in this component combine unit and integration testing approaches:
-   **Unit Tests:** For isolated logic within `LocalFileStorageService`.
-   **Integration Tests:** For scenarios involving actual file system operations, ensuring that files are written, read, and deleted correctly. These tests often clean up after themselves to maintain a consistent test environment.
