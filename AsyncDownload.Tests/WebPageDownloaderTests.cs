using System.Net;
using AsyncDownload.Models;
using AsyncDownload.Services;
using AsyncDownload.Interfaces;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace AsyncDownload.Tests;

public class WebPageDownloaderTests
{
    [Fact]
    public async Task DownloadPagesAsync_ShouldReturnSuccess_WhenValidHtml()
    {
        // Arrange
        var config = new DownloaderConfig { MaxConcurrentDownloads = 3, TimeoutSeconds = 5 };
        var html = "<html><title>Test Page</title><body>Hello World</body></html>";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new WebPageDownloader(httpClient, config);
        var reporter = new DummyProgressReporter();

        var urls = new List<string> { "http://test.com" };

        // Act
        var results = await downloader.DownloadPagesAsync(urls, reporter);

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].Title.Should().Be("Test Page");
    }

    private class DummyProgressReporter : IProgressReporter
    {
        public void Report(DownloadResult result) { /* no-op */ }
    }
}