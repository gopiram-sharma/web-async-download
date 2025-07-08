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
    private class DummyProgressReporter : IProgressReporter
    {
        public void Report(DownloadResult result) { /* no-op */ }
    }

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

    [Fact]
    public async Task DownloadPagesAsync_ShouldReturnFailure_WhenUrlIsEmpty()
    {
        var config = new DownloaderConfig { MaxConcurrentDownloads = 2, TimeoutSeconds = 2 };
        var httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
        var downloader = new WebPageDownloader(httpClient, config);
        var reporter = new DummyProgressReporter();

        var urls = new List<string> { "" };

        var results = await downloader.DownloadPagesAsync(urls, reporter);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().Contain("invalid");
    }

    [Fact]
    public async Task DownloadPagesAsync_ShouldReturnFailure_WhenContentTooLarge()
    {
        var config = new DownloaderConfig { MaxConcurrentDownloads = 2, TimeoutSeconds = 2, MaxContentBytes = 10 };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("01234567890123456789") // 20 bytes
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new WebPageDownloader(httpClient, config);
        var reporter = new DummyProgressReporter();

        var urls = new List<string> { "http://test.com" };

        var results = await downloader.DownloadPagesAsync(urls, reporter);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().MatchRegex("exceeded|too large");
    }

    [Fact]
    public async Task DownloadPagesAsync_ShouldReturnFailure_OnHttpError()
    {
        var config = new DownloaderConfig { MaxConcurrentDownloads = 2, TimeoutSeconds = 2 };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not found")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new WebPageDownloader(httpClient, config);
        var reporter = new DummyProgressReporter();

        var urls = new List<string> { "http://notfound.com" };

        var results = await downloader.DownloadPagesAsync(urls, reporter);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().MatchRegex("HTTP request error|404");
    }

    [Fact]
    public async Task DownloadPagesAsync_ShouldRespectTimeout()
    {
        var config = new DownloaderConfig { MaxConcurrentDownloads = 1, TimeoutSeconds = 1 };
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken token) =>
            {
                await Task.Delay(2000, token); // Simulate long delay
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html><title>Timeout</title></html>")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new WebPageDownloader(httpClient, config);
        var reporter = new DummyProgressReporter();

        var urls = new List<string> { "http://timeout.com" };

        var results = await downloader.DownloadPagesAsync(urls, reporter);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().MatchRegex("timed out|cancelled");
    }
}