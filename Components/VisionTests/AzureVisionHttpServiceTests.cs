using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vision.Configuration;
using Vision.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace VisionTests;

public class AzureVisionHttpServiceTests
{
    private readonly Mock<ILogger<AzureVisionHttpService>> _mockLogger;
    private readonly Mock<IOptions<AzureVisionOptions>> _mockOptions;

    public AzureVisionHttpServiceTests()
    {
        _mockLogger = new Mock<ILogger<AzureVisionHttpService>>();
        _mockOptions = new Mock<IOptions<AzureVisionOptions>>();
    }

    [Fact]
    public async Task AnalyzeImageAsync_SimulationEnabled_ReturnsDummyResult()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(new AzureVisionOptions { Enabled = true, Simulate = true });
        var httpClient = new HttpClient(new MockHttpMessageHandler("dummy")); // Mock HttpClient
        var service = new AzureVisionHttpService(httpClient, _mockLogger.Object, _mockOptions.Object);
        var imageData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.AnalyzeImageAsync(imageData);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("A simulated image analysis", result);
    }

    // Helper class to mock HttpMessageHandler
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;

        public MockHttpMessageHandler(string responseContent)
        {
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}