using AsyncDownload.Models;
using AsyncDownload.Services;
using FluentAssertions;

namespace AsyncDownload.Tests;

public class ContentAnalyzerTests
{
    [Fact]
    public void AnalyzePagesInParallel_ShouldReturnCorrectCounts()
    {
        // Arrange
        var analyzer = new ContentAnalyzer();
        var pages = new List<DownloadResult>
        {
            new DownloadResult
            {
                Url = "http://test.com",
                Content = "<html><body><img src='x'/><a href='y'>link</a> Hello world!</body></html>",
                Success = true
            }
        };

        // Act
        var stats = analyzer.AnalyzePagesInParallel(pages);

        // Assert
        stats.Should().HaveCount(1);
        stats[0].WordCount.Should().Be(3);
        stats[0].ImageCount.Should().Be(1);
        stats[0].LinkCount.Should().Be(1);
    }
}