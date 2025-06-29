using Domain.Entities;

namespace AzureVisionAnalysis.Models;

public class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScreenshotId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; } = 0;
    public AnalysisJobStatus Status { get; set; } = AnalysisJobStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    public static AnalysisJob Create(Guid screenshotId, string imagePath)
    {
        return new AnalysisJob
        {
            ScreenshotId = screenshotId,
            ImagePath = imagePath
        };
    }

    public void MarkAsProcessing()
    {
        Status = AnalysisJobStatus.Processing;
        AttemptCount++;
    }

    public void MarkAsCompleted()
    {
        Status = AnalysisJobStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = AnalysisJobStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool ShouldRetry(int maxAttempts)
    {
        return Status == AnalysisJobStatus.Failed && AttemptCount < maxAttempts;
    }
}

public enum AnalysisJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}