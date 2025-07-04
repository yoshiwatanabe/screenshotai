using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetEnv;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔍 Testing Azure Vision API Connection...");
        Console.WriteLine("==========================================");
        
        // Load environment variables
        try
        {
            Env.Load();
            Console.WriteLine("✅ Environment variables loaded");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading .env file: {ex.Message}");
            return;
        }

        // Get configuration
        var endpoint = Environment.GetEnvironmentVariable("AZURE_VISION_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_VISION_API_KEY");
        var enabled = Environment.GetEnvironmentVariable("AZURE_VISION_ENABLED");

        Console.WriteLine($"📍 Endpoint: {endpoint?.Substring(0, Math.Min(50, endpoint.Length ?? 0))}...");
        Console.WriteLine($"🔑 API Key: {(string.IsNullOrEmpty(apiKey) ? "❌ Missing" : $"✅ Present ({apiKey.Length} chars)")}");
        Console.WriteLine($"🎛️  Enabled: {enabled}");
        Console.WriteLine();

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("❌ Missing required environment variables!");
            Console.WriteLine("💡 Make sure AZURE_VISION_ENDPOINT and AZURE_VISION_API_KEY are set in .env");
            return;
        }

        if (enabled?.ToLower() != "true")
        {
            Console.WriteLine("⚠️  Azure Vision is disabled (AZURE_VISION_ENABLED=false)");
            Console.WriteLine("💡 Set AZURE_VISION_ENABLED=true in .env to enable testing");
            return;
        }

        // Create a simple test image (1x1 blue pixel PNG)
        var testImageData = CreateTestImage();
        Console.WriteLine($"🖼️  Created test image: {testImageData.Length} bytes");

        // Test the API
        await TestAzureVisionAPI(endpoint, apiKey, testImageData);
    }

    static async Task TestAzureVisionAPI(string endpoint, string apiKey, byte[] imageData)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            // Build the request URL (using Azure Vision 4.0 API)
            var requestUrl = $"{endpoint.TrimEnd('/')}/computervision/imageanalysis:analyze?api-version=2024-02-01&features=caption,tags&language=en&gender-neutral-caption=true";
            
            Console.WriteLine("🌐 Testing API connection...");
            Console.WriteLine($"📡 Request URL: {requestUrl}");

            // Create the request
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Content = new ByteArrayContent(imageData);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // Send the request
            Console.WriteLine("📤 Sending request to Azure Vision API...");
            using var response = await httpClient.SendAsync(request);

            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("🎉 SUCCESS! Azure Vision API is working!");
                Console.WriteLine();
                
                // Parse and display the response
                try
                {
                    using var document = JsonDocument.Parse(responseContent);
                    var root = document.RootElement;
                    
                    Console.WriteLine("📊 API Response Summary:");
                    
                    if (root.TryGetProperty("modelVersion", out var modelVersion))
                    {
                        Console.WriteLine($"   🤖 Model Version: {modelVersion.GetString()}");
                    }
                    
                    if (root.TryGetProperty("captionResult", out var captionResult) &&
                        captionResult.TryGetProperty("text", out var captionText))
                    {
                        var confidence = captionResult.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.0;
                        Console.WriteLine($"   📝 Caption: {captionText.GetString()} (confidence: {confidence:F2})");
                    }
                    
                    if (root.TryGetProperty("tagsResult", out var tagsResult) &&
                        tagsResult.TryGetProperty("values", out var tags))
                    {
                        Console.WriteLine($"   🏷️  Tags found: {tags.GetArrayLength()} tags");
                        var count = 0;
                        foreach (var tag in tags.EnumerateArray())
                        {
                            if (count >= 3) break; // Show first 3 tags
                            if (tag.TryGetProperty("name", out var tagName) &&
                                tag.TryGetProperty("confidence", out var tagConfidence))
                            {
                                Console.WriteLine($"      • {tagName.GetString()} ({tagConfidence.GetDouble():F2})");
                                count++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Could not parse response: {ex.Message}");
                    Console.WriteLine($"Raw response: {responseContent}");
                }
            }
            else
            {
                Console.WriteLine("❌ API Error!");
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                // Common error guidance
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine();
                    Console.WriteLine("💡 Troubleshooting for 401 Unauthorized:");
                    Console.WriteLine("   • Check your API key in .env file");
                    Console.WriteLine("   • Verify the key is active in Azure Portal");
                    Console.WriteLine("   • Try using Key 2 instead of Key 1");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine();
                    Console.WriteLine("💡 Troubleshooting for 404 Not Found:");
                    Console.WriteLine("   • Check your endpoint URL in .env file");
                    Console.WriteLine("   • Ensure the resource name is correct");
                    Console.WriteLine("   • Verify the resource is active in Azure Portal");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("💡 Troubleshooting:");
            Console.WriteLine("   • Check your internet connection");
            Console.WriteLine("   • Verify the endpoint URL is correct");
            Console.WriteLine("   • Ensure your firewall allows HTTPS connections");
        }
    }

    // Create a minimal test image (1x1 blue pixel PNG)
    static byte[] CreateTestImage()
    {
        // This is a minimal valid PNG file (1x1 blue pixel)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
            0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x04, 0x00, 0x00,
            0x04, 0x00, 0x01, 0x5C, 0xC2, 0xD5, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };
    }
}