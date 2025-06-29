using AzureVisionAnalysis.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AzureVisionAnalysis.Services;

public class AnalysisQueueService : BackgroundService, IAnalysisQueueService
{
    private readonly IAzureVisionService _visionService;
    private readonly ILogger<AnalysisQueueService> _logger;
    
    private readonly Channel<AnalysisJob> _jobQueue;
    private readonly ConcurrentDictionary<Guid, AnalysisJob> _jobs;
    private readonly ConcurrentDictionary<Guid, VisionAnalysisResult> _results;
    
    private readonly object _statusLock = new();
    private bool _isProcessing;
    private DateTime? _lastProcessedAt;

    public event EventHandler<AnalysisCompletedEventArgs>? AnalysisCompleted;

    public AnalysisQueueService(IAzureVisionService visionService, ILogger<AnalysisQueueService> logger)
    {
        _visionService = visionService;
        _logger = logger;
        
        // Create unbounded channel for job queue
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        
        _jobQueue = Channel.CreateUnbounded<AnalysisJob>(options);
        _jobs = new ConcurrentDictionary<Guid, AnalysisJob>();
        _results = new ConcurrentDictionary<Guid, VisionAnalysisResult>();
        
        _logger.LogInformation("Analysis Queue Service initialized");
    }

    public async Task<AnalysisJob> QueueAnalysisAsync(Guid screenshotId, string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var job = AnalysisJob.Create(screenshotId, imagePath);
            
            // Store job in tracking dictionary
            _jobs.TryAdd(job.Id, job);
            
            // Queue for processing
            await _jobQueue.Writer.WriteAsync(job, cancellationToken);
            
            _logger.LogDebug("Queued analysis job {JobId} for screenshot {ScreenshotId}: {ImagePath}", 
                job.Id, screenshotId, imagePath);
            
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing analysis for screenshot {ScreenshotId}", screenshotId);
            throw;
        }
    }

    public async Task<AnalysisQueueStatus> GetQueueStatusAsync()
    {
        var jobs = _jobs.Values.ToList();
        
        lock (_statusLock)
        {
            return new AnalysisQueueStatus
            {
                QueuedJobs = jobs.Count(j => j.Status == AnalysisJobStatus.Queued),
                ProcessingJobs = jobs.Count(j => j.Status == AnalysisJobStatus.Processing),
                CompletedJobs = jobs.Count(j => j.Status == AnalysisJobStatus.Completed),
                FailedJobs = jobs.Count(j => j.Status == AnalysisJobStatus.Failed),
                IsProcessing = _isProcessing,
                LastProcessedAt = _lastProcessedAt
            };
        }
    }

    public async Task<VisionAnalysisResult?> GetAnalysisResultAsync(Guid screenshotId)
    {
        return _results.TryGetValue(screenshotId, out var result) ? result : null;
    }

    public async Task<List<(Guid ScreenshotId, VisionAnalysisResult Result)>> GetAllCompletedAnalysesAsync()
    {
        return _results.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }

    public Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting analysis queue processing");
        return StartAsync(cancellationToken);
    }

    public Task StopProcessingAsync()
    {
        _logger.LogInformation("Stopping analysis queue processing");
        return StopAsync(CancellationToken.None);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis queue background service started");
        
        lock (_statusLock)
        {
            _isProcessing = true;
        }

        try
        {
            await foreach (var job in _jobQueue.Reader.ReadAllAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await ProcessJobAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analysis queue processing was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analysis queue processing");
        }
        finally
        {
            lock (_statusLock)
            {
                _isProcessing = false;
            }
            
            _logger.LogInformation("Analysis queue background service stopped");
        }
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing analysis job {JobId} for screenshot {ScreenshotId}", 
                job.Id, job.ScreenshotId);
            
            // Mark job as processing
            job.MarkAsProcessing();
            
            // Perform the analysis
            var result = await _visionService.AnalyzeImageAsync(job.ImagePath, cancellationToken);
            
            if (result.Success)
            {
                // Store result and mark job as completed
                _results.TryAdd(job.ScreenshotId, result);
                job.MarkAsCompleted();
                
                _logger.LogInformation("Completed analysis for screenshot {ScreenshotId}. Description: {Description}", 
                    job.ScreenshotId, result.MainCaption);
                
                // Fire completion event
                OnAnalysisCompleted(new AnalysisCompletedEventArgs
                {
                    ScreenshotId = job.ScreenshotId,
                    Result = result,
                    Success = true
                });
            }
            else
            {
                // Mark job as failed
                job.MarkAsFailed(result.ErrorMessage ?? "Unknown error");
                
                _logger.LogWarning("Analysis failed for screenshot {ScreenshotId}: {Error}", 
                    job.ScreenshotId, result.ErrorMessage);
                
                // Fire completion event (with failure)
                OnAnalysisCompleted(new AnalysisCompletedEventArgs
                {
                    ScreenshotId = job.ScreenshotId,
                    Result = result,
                    Success = false
                });
            }
            
            lock (_statusLock)
            {
                _lastProcessedAt = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Analysis job {JobId} was cancelled", job.Id);
            job.MarkAsFailed("Analysis was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing analysis job {JobId} for screenshot {ScreenshotId}", 
                job.Id, job.ScreenshotId);
            
            job.MarkAsFailed($"Processing error: {ex.Message}");
            
            OnAnalysisCompleted(new AnalysisCompletedEventArgs
            {
                ScreenshotId = job.ScreenshotId,
                Result = VisionAnalysisResult.CreateFailure(ex.Message),
                Success = false
            });
        }
    }

    private void OnAnalysisCompleted(AnalysisCompletedEventArgs args)
    {
        try
        {
            AnalysisCompleted?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing AnalysisCompleted event for screenshot {ScreenshotId}", 
                args.ScreenshotId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down analysis queue service...");
        
        // Complete the channel to stop accepting new jobs
        _jobQueue.Writer.Complete();
        
        // Stop the background service
        await base.StopAsync(cancellationToken);
        
        lock (_statusLock)
        {
            _isProcessing = false;
        }
        
        _logger.LogInformation("Analysis queue service shutdown complete");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _jobQueue.Writer.Complete();
        }
        
        base.Dispose(disposing);
    }
}