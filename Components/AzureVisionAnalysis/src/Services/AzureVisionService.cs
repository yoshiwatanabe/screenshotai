using Azure;
using Azure.AI.Vision.ImageAnalysis;
using AzureVisionAnalysis.Configuration;
using AzureVisionAnalysis.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AzureVisionAnalysis.Services;

public class AzureVisionService : IAzureVisionService
{
    private readonly AzureVisionOptions _options;
    private readonly ILogger<AzureVisionService> _logger;
    private readonly ImageAnalysisClient _client;

    public AzureVisionService(IOptions<AzureVisionOptions> options, ILogger<AzureVisionService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Validate configuration
        _options.Validate();
        
        // Initialize Azure Vision client
        var credential = new AzureKeyCredential(_options.ApiKey);
        _client = new ImageAnalysisClient(new Uri(_options.Endpoint), credential);
        
        _logger.LogInformation("Azure Vision Service initialized with endpoint: {Endpoint}", _options.Endpoint);
    }

    public async Task<VisionAnalysisResult> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting analysis for image: {ImagePath}", imagePath);
            
            // Validate image file
            if (!await CanAnalyzeImageAsync(imagePath))
            {
                return VisionAnalysisResult.CreateFailure("Image file is not suitable for analysis");
            }

            // Read image data
            var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            
            var result = await AnalyzeImageAsync(imageData, cancellationToken);
            stopwatch.Stop();
            
            result.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogInformation("Image analysis completed in {Duration}ms: {ImagePath}", 
                stopwatch.ElapsedMilliseconds, imagePath);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image: {ImagePath}", imagePath);
            return VisionAnalysisResult.CreateFailure($"Analysis failed: {ex.Message}");
        }
    }

    public async Task<VisionAnalysisResult> AnalyzeImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting analysis for image data ({Size} bytes)", imageData.Length);
            
            // Validate image size
            if (imageData.Length > _options.MaxFileSizeBytes)
            {
                return VisionAnalysisResult.CreateFailure(
                    $"Image size ({imageData.Length} bytes) exceeds maximum allowed size ({_options.MaxFileSizeBytes} bytes)");
            }

            // Configure analysis features
            var features = GetAnalysisFeatures();
            var analysisOptions = new ImageAnalysisOptions
            {
                GenderNeutralCaption = true,
                Features = features
            };

            // Perform analysis with retry logic
            var analysisResult = await PerformAnalysisWithRetryAsync(imageData, analysisOptions, cancellationToken);
            
            // Convert to our result format
            var result = ConvertToVisionAnalysisResult(analysisResult);
            
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogDebug("Image analysis completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Image analysis was cancelled");
            return VisionAnalysisResult.CreateFailure("Analysis was cancelled");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Vision API request failed: {ErrorCode}", ex.ErrorCode);
            return VisionAnalysisResult.CreateFailure($"Azure Vision API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image analysis");
            return VisionAnalysisResult.CreateFailure($"Analysis failed: {ex.Message}");
        }
    }

    public async Task<(string caption, double confidence)> GetImageCaptionAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            
            var analysisOptions = new ImageAnalysisOptions
            {
                GenderNeutralCaption = true,
                Features = ImageAnalysisFeature.Caption
            };

            var result = await _client.AnalyzeAsync(BinaryData.FromBytes(imageData), analysisOptions, cancellationToken);
            
            if (result?.Value?.Caption != null)
            {
                return (result.Value.Caption.Text, result.Value.Caption.Confidence);
            }
            
            return ("No caption available", 0.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image caption: {ImagePath}", imagePath);
            return ($"Error: {ex.Message}", 0.0);
        }
    }

    public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a minimal test image (1x1 pixel PNG)
            var testImageData = CreateTestImage();
            
            var analysisOptions = new ImageAnalysisOptions
            {
                Features = ImageAnalysisFeature.Caption
            };

            var result = await _client.AnalyzeAsync(BinaryData.FromBytes(testImageData), analysisOptions, cancellationToken);
            
            _logger.LogDebug("Azure Vision service availability check passed");
            return result?.Value != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Vision service is not available");
            return false;
        }
    }

    public async Task<bool> CanAnalyzeImageAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Image file does not exist: {ImagePath}", imagePath);
                return false;
            }

            var fileInfo = new FileInfo(imagePath);
            
            if (fileInfo.Length > _options.MaxFileSizeBytes)
            {
                _logger.LogWarning("Image file too large: {Size} bytes (max: {MaxSize})", 
                    fileInfo.Length, _options.MaxFileSizeBytes);
                return false;
            }

            if (fileInfo.Length == 0)
            {
                _logger.LogWarning("Image file is empty: {ImagePath}", imagePath);
                return false;
            }

            // Check file extension
            var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            var extension = fileInfo.Extension.ToLowerInvariant();
            
            if (!supportedExtensions.Contains(extension))
            {
                _logger.LogWarning("Unsupported image format: {Extension}", extension);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if image can be analyzed: {ImagePath}", imagePath);
            return false;
        }
    }

    private ImageAnalysisFeature GetAnalysisFeatures()
    {
        var features = ImageAnalysisFeature.Caption;

        if (_options.IncludeDenseCaptions)
            features |= ImageAnalysisFeature.DenseCaptions;

        if (_options.IncludeObjects)
            features |= ImageAnalysisFeature.Objects;

        if (_options.IncludeText)
            features |= ImageAnalysisFeature.Text;

        if (_options.IncludeTags)
            features |= ImageAnalysisFeature.Tags;

        return features;
    }

    private async Task<ImageAnalysisResult> PerformAnalysisWithRetryAsync(
        byte[] imageData, 
        ImageAnalysisOptions options, 
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogDebug("Analysis attempt {Attempt}/{MaxAttempts}", attempt, _options.MaxRetryAttempts);
                
                var result = await _client.AnalyzeAsync(BinaryData.FromBytes(imageData), options, cancellationToken);
                
                if (result?.Value != null)
                {
                    return result.Value;
                }
                
                throw new InvalidOperationException("Analysis returned null result");
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt >= _options.MaxRetryAttempts)
                {
                    break;
                }
                
                _logger.LogWarning(ex, "Analysis attempt {Attempt} failed, retrying...", attempt);
                
                var delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs * attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Analysis failed after all retry attempts");
    }

    private VisionAnalysisResult ConvertToVisionAnalysisResult(ImageAnalysisResult analysisResult)
    {
        var result = new VisionAnalysisResult
        {
            Success = true,
            AnalyzedAt = DateTime.UtcNow
        };

        // Main caption
        if (analysisResult.Caption != null)
        {
            result.MainCaption = analysisResult.Caption.Text;
            result.MainCaptionConfidence = analysisResult.Caption.Confidence;
        }

        // Dense captions
        if (analysisResult.DenseCaptions != null)
        {
            result.DenseCaptions = analysisResult.DenseCaptions.Values
                .OrderByDescending(dc => dc.Confidence)
                .Select(dc => dc.Text)
                .ToList();
        }

        // Objects
        if (analysisResult.Objects != null)
        {
            result.Objects = analysisResult.Objects.Values
                .Select(obj => new DetectedObject
                {
                    Name = obj.Tags.First().Name,
                    Confidence = obj.Tags.First().Confidence,
                    BoundingBox = new BoundingBox
                    {
                        X = obj.BoundingBox.X,
                        Y = obj.BoundingBox.Y,
                        Width = obj.BoundingBox.Width,
                        Height = obj.BoundingBox.Height
                    }
                })
                .ToList();
        }

        // Tags
        if (analysisResult.Tags != null)
        {
            result.Tags = analysisResult.Tags.Values
                .Where(tag => tag.Confidence > 0.5)
                .OrderByDescending(tag => tag.Confidence)
                .Select(tag => tag.Name)
                .ToList();
        }

        // Extracted text
        if (analysisResult.Text != null)
        {
            result.ExtractedText = string.Join(" ", analysisResult.Text.Blocks
                .SelectMany(block => block.Lines)
                .Select(line => line.Text));
        }

        return result;
    }

    private byte[] CreateTestImage()
    {
        // Create a minimal 1x1 pixel PNG for service availability testing
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChAI9jU77wwAAAABJRU5ErkJggg==");
    }
}