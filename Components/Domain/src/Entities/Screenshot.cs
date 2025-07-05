using ScreenshotManager.Domain.Enums;

namespace ScreenshotManager.Domain.Entities;

public class Screenshot
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public string BlobName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ScreenshotSource Source { get; private set; }
    public ScreenshotStatus Status { get; private set; }

    public string? ExtractedText { get; private set; }
    public List<string> Tags { get; private set; }
    public string? FailureReason { get; private set; }

    private Screenshot(Guid id, string displayName, string blobName, DateTime createdAt, ScreenshotSource source)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        Id = id;
        DisplayName = displayName;
        BlobName = blobName;
        CreatedAt = createdAt;
        Source = source;
        Status = ScreenshotStatus.Processing;
        Tags = new List<string>();
    }

    public static Screenshot CreateFromClipboard(string displayName, string blobName)
    {
        return new Screenshot(
            id: Guid.NewGuid(),
            displayName: displayName,
            blobName: blobName,
            createdAt: DateTime.UtcNow,
            source: ScreenshotSource.Clipboard
        );
    }

    public static Screenshot CreateFromUpload(string displayName, string blobName)
    {
        return new Screenshot(
            id: Guid.NewGuid(),
            displayName: displayName,
            blobName: blobName,
            createdAt: DateTime.UtcNow,
            source: ScreenshotSource.FileUpload
        );
    }

    public static Screenshot CreateFromDragDrop(string displayName, string blobName)
    {
        return new Screenshot(
            id: Guid.NewGuid(),
            displayName: displayName,
            blobName: blobName,
            createdAt: DateTime.UtcNow,
            source: ScreenshotSource.DragDrop
        );
    }

    public static Screenshot CreateFromAPI(string displayName, string blobName)
    {
        return new Screenshot(
            id: Guid.NewGuid(),
            displayName: displayName,
            blobName: blobName,
            createdAt: DateTime.UtcNow,
            source: ScreenshotSource.API
        );
    }

    public void UpdateDisplayName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Display name cannot be null or empty", nameof(newName));

        if (Status == ScreenshotStatus.Deleted)
            throw new InvalidOperationException("Cannot update display name of a deleted screenshot");

        DisplayName = newName;
    }

    public void MarkAsProcessed()
    {
        if (Status == ScreenshotStatus.Deleted)
            throw new InvalidOperationException("Cannot mark a deleted screenshot as processed");

        Status = ScreenshotStatus.Ready;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason cannot be null or empty", nameof(reason));

        if (Status == ScreenshotStatus.Deleted)
            throw new InvalidOperationException("Cannot mark a deleted screenshot as failed");

        Status = ScreenshotStatus.Failed;
        FailureReason = reason;
    }

    public void MarkAsDeleted()
    {
        Status = ScreenshotStatus.Deleted;
    }

    public void AddAIAnalysis(string extractedText, List<string> tags)
    {
        if (Status == ScreenshotStatus.Deleted)
            throw new InvalidOperationException("Cannot add AI analysis to a deleted screenshot");

        if (Status == ScreenshotStatus.Failed)
            throw new InvalidOperationException("Cannot add AI analysis to a failed screenshot");

        ExtractedText = extractedText ?? string.Empty;
        Tags = tags?.ToList() ?? new List<string>();
    }

    public bool HasAIAnalysis => !string.IsNullOrEmpty(ExtractedText) || Tags.Any();

    public bool IsProcessing => Status == ScreenshotStatus.Processing;
    public bool IsReady => Status == ScreenshotStatus.Ready;
    public bool IsFailed => Status == ScreenshotStatus.Failed;
    public bool IsDeleted => Status == ScreenshotStatus.Deleted;
}