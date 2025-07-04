# DomainTests Component

## Overview
The `DomainTests` component is a .NET class library dedicated to housing unit tests for the `Domain` component. It ensures the correctness, integrity, and expected behavior of the core domain entities, value objects, and enumerations defined within the `Domain` project.

## Key Features
-   **Unit Testing:** Contains focused, isolated tests for individual units of code within the `Domain` component.
-   **Validation of Business Rules:** Verifies that the business rules and invariants encapsulated in domain objects are correctly enforced.
-   **Data Integrity Checks:** Ensures that value objects and entities maintain their integrity under various conditions.

## Dependencies
This component depends on:
-   `Components/Domain` (the project being tested).
-   `Microsoft.NET.Test.Sdk` for the test SDK.
-   `NUnit` for the testing framework.
-   `NUnit3TestAdapter` for running NUnit tests.
-   `Microsoft.NET.Test.Sdk` for the test SDK.

## Usage
These tests are typically run using a test runner integrated into an IDE (like Visual Studio Code with C# extensions) or from the command line.

To run tests from the command line:

```bash
dotnet test Components/DomainTests/DomainTests.csproj
```

## Testing Strategy
Tests in this component follow a unit testing methodology, focusing on the smallest testable parts of the `Domain` logic. They aim to cover:
-   Constructor logic and property assignments.
-   Equality and inequality comparisons for value objects.
-   Validation rules and error conditions.
-   Behavior of methods within domain entities.
