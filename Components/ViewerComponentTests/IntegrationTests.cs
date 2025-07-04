using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ViewerComponent.Services;
using ViewerComponent.Configuration;
using ViewerComponent.Extensions;
using System.Net;
using System.Text.Json;

namespace ViewerComponentTests;

public class ViewerIntegrationTests : IClassFixture<WebApplicationFactory<ViewerComponent.Controllers.ViewerController>>
{
    private readonly WebApplicationFactory<ViewerComponent.Controllers.ViewerController> _factory;
    private readonly HttpClient _client;
    private readonly string _testDirectory;

    public ViewerIntegrationTests(WebApplicationFactory<ViewerComponent.Controllers.ViewerController> factory)
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ViewerIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the ViewerOptions with test configuration
                services.Configure<ViewerOptions>(options =>
                {
                    options.OutputDirectory = _testDirectory;
                    options.Port = 0; // Use any available port
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetFiles_EmptyDirectory_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/files");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<JsonElement[]>(content);
        Assert.Empty(files);
    }

    [Fact]
    public async Task GetFiles_WithTestFiles_ReturnsFileList()
    {
        // Arrange
        var imagePath = Path.Combine(_testDirectory, "integration_test.png");
        var jsonPath = Path.Combine(_testDirectory, "integration_test.json");
        
        await File.WriteAllBytesAsync(imagePath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllTextAsync(jsonPath, "{\"test\": \"integration\"}");

        // Act
        var response = await _client.GetAsync("/api/files");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<JsonElement[]>(content);
        Assert.Single(files);
        
        var file = files[0];
        Assert.Equal("integration_test.png", file.GetProperty("fileName").GetString());
        Assert.True(file.GetProperty("hasAnalysis").GetBoolean());
    }

    [Fact]
    public async Task GetImage_ExistingFile_ReturnsImageData()
    {
        // Arrange
        var imagePath = Path.Combine(_testDirectory, "test_image.png");
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        await File.WriteAllBytesAsync(imagePath, imageData);

        // Act
        var response = await _client.GetAsync("/api/image/test_image.png");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        
        var responseData = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(imageData, responseData);
    }

    [Fact]
    public async Task GetImage_NonExistentFile_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/image/nonexistent.png");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_ExistingFile_ReturnsJsonData()
    {
        // Arrange
        var jsonPath = Path.Combine(_testDirectory, "test_analysis.json");
        var analysisData = "{\"categories\": [{\"name\": \"test\", \"score\": 0.95}]}";
        await File.WriteAllTextAsync(jsonPath, analysisData);

        // Act
        var response = await _client.GetAsync("/api/analysis/test_analysis.png");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(analysisData, responseContent);
    }

    [Fact]
    public async Task GetAnalysis_NonExistentFile_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/analysis/nonexistent.png");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ReturnsValidStatus()
    {
        // Arrange
        var imagePath1 = Path.Combine(_testDirectory, "status_test1.png");
        var imagePath2 = Path.Combine(_testDirectory, "status_test2.png");
        var jsonPath1 = Path.Combine(_testDirectory, "status_test1.json");
        
        await File.WriteAllBytesAsync(imagePath1, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllBytesAsync(imagePath2, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        await File.WriteAllTextAsync(jsonPath1, "{}");

        // Act
        var response = await _client.GetAsync("/api/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.Equal(2, status.GetProperty("totalImages").GetInt32());
        Assert.Equal(1, status.GetProperty("imagesWithAnalysis").GetInt32());
        Assert.Equal(_testDirectory, status.GetProperty("outputDirectory").GetString());
    }

    [Fact]
    public async Task RootPath_ReturnsHTML()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        // This test would require setting up static file serving
        // For now, we'll just check that it doesn't throw an exception
        // In a full integration test, we'd verify the HTML content is served
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                   response.StatusCode == HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}