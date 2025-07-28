using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

namespace AsyncDownload.Services;

/// Pros of Channel<T>:
/// - Lightweight producer-consumer pattern
/// - Fine-grained control over backpressure
/// - Flexible for custom pipelines
/// 
/// Cons:
/// - No built-in transformations (manual processing required)

public class ChannelDownloader
{
    private readonly HttpClient _httpClient;
    private readonly DownloaderConfig _config;
    private readonly int _maxRetries;

    public ChannelDownloader(HttpClient httpClient, DownloaderConfig config, int maxRetries = 3)
    {
        if (config.MaxConcurrentDownloads <= 0)
            throw new ArgumentException("MaxConcurrentDownloads must be positive.");
        if (config.TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be positive.");
        if (config.MaxContentBytes <= 0)
            throw new ArgumentException("MaxContentBytes must be positive.");

        _maxRetries = config.MaxRetries <= 0 ? 1 : config.MaxRetries;
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<DownloadResult>> DownloadPagesAsync(IEnumerable<string> urls, IProgressReporter progressReporter, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(_config.MaxConcurrentDownloads)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var results = new List<DownloadResult>();
        var resultsLock = new object();

        var writer = Task.Run(async () =>
        {
            try
            {
                foreach (var url in urls)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await channel.Writer.WriteAsync(url, cancellationToken);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        var readers = new Task[_config.MaxConcurrentDownloads];
        for (int i = 0; i < readers.Length; i++)
        {
            readers[i] = Task.Run(async () =>
            {
                await foreach (var url in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    var downloadResult = await DownloadWithRetryAsync(url, cancellationToken);

                    if (downloadResult.Success && !string.IsNullOrEmpty(downloadResult.Content))
                    {
                        downloadResult.Title = ExtractTitle(downloadResult.Content);
                    }
                    else
                    {
                        downloadResult.Title = "No Title (due to download failure)";
                    }

                    progressReporter.Report(downloadResult);

                    lock (resultsLock)
                    {
                        results.Add(downloadResult);
                    }
                }
            }, cancellationToken);
        }

        await Task.WhenAll(writer);
        await Task.WhenAll(readers);

        return results;
    }

    private async Task<DownloadResult> DownloadWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        var result = new DownloadResult { Url = url };
        int attempt = 0;

        while (attempt <= _maxRetries)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await _httpClient.GetStringAsync(url, cancellationToken);
                result.Content = content;
                result.Success = true;
                return result;
            }
            catch (HttpRequestException ex)
            {
                attempt++;
                result.ErrorMessage = $"Attempt {attempt}: {ex.Message}";
                if (attempt > _maxRetries)
                {
                    result.Success = false;
                    return result;
                }
                await Task.Delay(500, cancellationToken); // Backoff
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Download cancelled by token.";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return result;
            }
        }

        return result;
    }

    private string ExtractTitle(string html)
    {
        var match = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "No Title Found";
    }
}
