
# Image Analysis Service: Architecture and Design

This document outlines the architecture for a Windows background service that monitors a directory for new images, analyzes them using Azure AI Vision, and stores the results. This approach pivots from the original goal of building a custom screen capture tool.

## 1. High-Level Architecture

The application will be a **.NET Worker Service** configured to run as a long-running, background Windows Service.

The core data flow is as follows:
1.  A `FileSystemWatcher` instance monitors a designated folder for new image files (`.png`, `.jpg`, etc.).
2.  When a new image is detected, its file path is added to a thread-safe, in-memory queue. This decouples file detection from processing, ensuring that bursts of new files are handled gracefully without missing any.
3.  A background processing task continuously pulls file paths from the queue.
4.  For each file, the processor invokes an analysis service, which sends the image to the Azure AI Vision API.
5.  The returned analysis (e.g., description, tags, text content) is saved as a JSON "sidecar" file in the same directory as the image (e.g., for `image1.png`, the analysis is saved to `image1.json`).

## 2. Core Components & Responsibilities

### New Project: `ImageAnalysisService` (.NET Worker)

This will be the main host project for the application.

| Component                 | Responsibility                                                                                                                            | Implementation Details                                     |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| **Program.cs**            | Configures and runs the .NET Generic Host. Sets up dependency injection, configuration (`appsettings.json`), and logging. Registers the worker to run as a Windows Service. | `Host.CreateDefaultBuilder().UseWindowsService()`          |
| **FolderMonitorWorker**   | A `BackgroundService` that initializes the `FileSystemWatcher` and registers for the `Created` event. Enqueues new file paths for processing. | `System.IO.FileSystemWatcher`                              |
| **ProcessingChannel**     | A singleton service that holds a thread-safe queue for file paths.                                                                        | `System.Threading.Channels.Channel<string>` or `BlockingCollection<string>` |
| **ImageProcessorWorker**  | A `BackgroundService` that dequeues file paths from the `ProcessingChannel` and orchestrates the analysis and storage steps.                | `BackgroundService` loop calling other services.           |
| **VisionAnalysisService** | Communicates with the Azure AI Vision API.                                                                                                | Reuses/adapts `AzureVisionHttpService` from `Storage` component. |
| **AnalysisStorageService**| Saves the analysis results to the file system.                                                                                            | Reuses/adapts `LocalFileStorageService` from `Storage` component. |

## 3. Reusable and Adaptable Components

The following existing components are valuable and will be reused or adapted for the new architecture.

| Component/Project           | Status      | Notes                                                                                                                                                           |
| --------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Components/Storage**      | **Reusable**| This is the core of the processing logic. `AzureVisionHttpService` and `LocalFileStorageService` are directly applicable to the new design.                      |
| **Components/Domain**       | **Reusable**| The `Screenshot` entity can be repurposed to represent any image being processed, not just a screenshot. The `BlobReference` value object is also still useful. |
| **Components/GalleryViewer**| **Adaptable** | This UI component could be repurposed in a separate application to view the images and their corresponding JSON analysis files, creating a searchable gallery. |
| **StorageTests**            | **Reusable**| The tests for the storage services remain valid and essential.                                                                                                  |

## 4. Obsolete Components (To Be Removed)

The pivot to a file-monitoring service makes the following components related to direct screen capture obsolete. They should be removed from the solution to simplify the codebase.

| Component/Project                | Status      | Reason for Removal                                                                                                                            |
| -------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| **Components/ScreenshotCapture** | **Obsolete**| All services related to area selection, global hotkeys, and capturing the screen are no longer needed. This is the primary component to be removed. |
| **ScreenshotConsoleService**     | **Obsolete**| This was a console-based implementation of the capture functionality.                                                                         |
| **ScreenshotManagerApp**         | **Obsolete**| The main WPF application project that orchestrated the screen capture flow. A new UI (like an enhanced `GalleryViewer`) might be created later, but this project is no longer needed. |
| **GalleryLauncher**              | **Obsolete**| The launcher for the main application.                                                                                                          |

By focusing on this streamlined architecture, the project can deliver its core value proposition—AI-powered image analysis and indexing—more efficiently.
