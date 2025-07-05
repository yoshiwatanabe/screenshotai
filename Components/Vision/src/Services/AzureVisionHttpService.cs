using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Vision.Configuration;

namespace Vision.Services;

public class AzureVisionHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureVisionHttpService> _logger;
    private readonly AzureVisionOptions _options;

    public AzureVisionHttpService(
        HttpClient httpClient,
        ILogger<AzureVisionHttpService> logger,
        IOptions<AzureVisionOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        // Directly set BaseAddress here for debugging purposes
        if (!string.IsNullOrEmpty(_options.Endpoint))
        {
            _httpClient.BaseAddress = new Uri(_options.Endpoint);
            _logger.LogDebug($"HttpClient BaseAddress set directly in constructor to: {_httpClient.BaseAddress}");
        }
    }

    public async Task<string?> AnalyzeImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Azure Vision API is disabled. Skipping analysis.");
            return null;
        }

        if (_options.Simulate)
        {
            _logger.LogInformation("Azure Vision API simulation enabled. Returning dummy analysis result.");
            // Return a dummy JSON string that mimics a successful Azure Vision API response
            return "{\"captionResult\":{\"text\":\"A simulated image analysis\",\"confidence\":0.95},\"tagsResult\":{\"values\":[{\"name\":\"simulated\",\"confidence\":0.99},{\"name\":\"test\",\"confidence\":0.8}]},\"readResult\":{\"blocks\":[{\"lines\":[{\"text\":\"Simulated text from image\"}]}]},\"objectsResult\":{\"values\":[{\"tags\":[{\"name\":\"simulated object\",\"confidence\":0.9}]}]},\"peopleResult\":{\"values\":[{},{}]}}";
        }

        try
        {
            // Build relative path and query parameters according to 4.0 API specification
            var relativePath = "computervision/imageanalysis:analyze";
            var queryParams = new List<string>
            {
                "api-version=2024-02-01",
                $"features={string.Join(",", _options.Features)}",
                $"language={_options.Language}",
                $"gender-neutral-caption={_options.GenderNeutralCaption.ToString().ToLower()}"
            };

            var requestUri = $"{relativePath}?{string.Join("&", queryParams)}";

            _logger.LogDebug($"Azure Vision Endpoint: {_options.Endpoint}");
            _logger.LogDebug($"Azure Vision API Key (length: {_options.ApiKey.Length}): {(_options.ApiKey.Length > 4 ? "..." + _options.ApiKey.Substring(_options.ApiKey.Length - 4) : "(empty)")}");
            _logger.LogDebug($"Request URI: {requestUri}");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            // Set headers according to 4.0 API specification
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
            request.Content = new ByteArrayContent(imageData);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            _logger.LogDebug("Sending image analysis request to Azure Vision API 4.0");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Azure Vision API returned error {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var analysisResult = ExtractDescription(jsonResponse);

            _logger.LogInformation("Successfully analyzed image with Azure Vision API 4.0");
            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure Vision API");
            return null;
        }
    }

    private string ExtractDescription(string jsonResponse)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;

            var descriptions = new List<string>();

            // Extract model version for logging
            if (root.TryGetProperty("modelVersion", out var modelVersion))
            {
                _logger.LogDebug("Using Azure Vision model version: {ModelVersion}", modelVersion.GetString());
            }

            // Extract caption (4.0 API structure)
            if (root.TryGetProperty("captionResult", out var captionResult) &&
                captionResult.TryGetProperty("text", out var captionText))
            {
                var confidence = captionResult.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0;
                descriptions.Add($"Caption: {captionText.GetString()} (confidence: {confidence:F2})");
            }

            // Extract tags (4.0 API structure)
            if (root.TryGetProperty("tagsResult", out var tagsResult) &&
                tagsResult.TryGetProperty("values", out var tags))
            {
                var tagNames = new List<string>();
                foreach (var tag in tags.EnumerateArray())
                {
                    if (tag.TryGetProperty("name", out var tagName) &&
                        tag.TryGetProperty("confidence", out var tagConfidence))
                    {
                        var confidence = tagConfidence.GetDouble();
                        // Only include tags with reasonable confidence
                        if (confidence > _options.MinConfidenceThreshold)
                        {
                            tagNames.Add($"{tagName.GetString()}({confidence:F1})");
                        }
                    }
                }
                if (tagNames.Count > 0)
                {
                    descriptions.Add($"Tags: {string.Join(", ", tagNames.Take(8))}");
                }
            }

            // Extract detected objects (4.0 API structure)
            if (root.TryGetProperty("objectsResult", out var objectsResult) &&
                objectsResult.TryGetProperty("values", out var objects))
            {
                var objectNames = new List<string>();
                foreach (var obj in objects.EnumerateArray())
                {
                    if (obj.TryGetProperty("tags", out var objTags) && objTags.GetArrayLength() > 0)
                    {
                        var firstTag = objTags[0];
                        if (firstTag.TryGetProperty("name", out var objName) &&
                            firstTag.TryGetProperty("confidence", out var objConfidence))
                        {
                            var confidence = objConfidence.GetDouble();
                            if (confidence > _options.MinConfidenceThreshold)
                            {
                                objectNames.Add(objName.GetString()!);
                            }
                        }
                    }
                }
                if (objectNames.Count > 0)
                {
                    descriptions.Add($"Objects: {string.Join(", ", objectNames.Distinct().Take(5))}");
                }
            }

            // Extract detected text (4.0 API structure)
            if (root.TryGetProperty("readResult", out var readResult) &&
                readResult.TryGetProperty("blocks", out var blocks))
            {
                var textLines = new List<string>();
                foreach (var block in blocks.EnumerateArray())
                {
                    if (block.TryGetProperty("lines", out var lines))
                    {
                        foreach (var line in lines.EnumerateArray())
                        {
                            if (line.TryGetProperty("text", out var lineText))
                            {
                                textLines.Add(lineText.GetString()!);
                            }
                        }
                    }
                }
                if (textLines.Count > 0)
                {
                    descriptions.Add($"Text: {string.Join(" ", textLines)}");
                }
            }

            // Extract people detection (4.0 API structure)
            if (root.TryGetProperty("peopleResult", out var peopleResult) &&
                peopleResult.TryGetProperty("values", out var people))
            {
                var peopleCount = people.GetArrayLength();
                if (peopleCount > 0)
                {
                    descriptions.Add($"People: {peopleCount} person{(peopleCount > 1 ? "s" : "")} detected");
                }
            }

            return descriptions.Count > 0 ? string.Join(" | ", descriptions) : "No description available";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Azure Vision API 4.0 response");
            return "Error parsing analysis result";
        }
    }
}

