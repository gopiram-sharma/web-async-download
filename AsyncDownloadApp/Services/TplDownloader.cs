using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Text.RegularExpressions;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

/// Pros of TPL Dataflow:
/// - Pipelines with multiple transformations/stages
/// - Fine-grained concurrency control per stage
/// - Backpressure (blocks when capacity is full)
/// - Built-in completion and fault handling
///
/// Cons:
/// - Extra dependency in .NET Core/.NET Standard (needs System.Threading.Tasks.Dataflow NuGet)
/// - More complex than simpler async/await patterns if you don't need pipelines

namespace AsyncDownload.Services;

public class TplDownloader
{
    private readonly HttpClient _httpClient;
    private readonly DownloaderConfig _config;
    private readonly int _maxRetries;

    public TplDownloader(HttpClient httpClient, DownloaderConfig config)
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
        var results = new List<DownloadResult>();
        var resultsLock = new object();

        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = _config.MaxConcurrentDownloads,
            BoundedCapacity = _config.TimeoutSeconds,
            CancellationToken = cancellationToken
        };

        var downloadBlock = new TransformBlock<string, DownloadResult>(async url =>
        {
            return await DownloadWithRetryAsync(url, cancellationToken);
        }, options);

        var parseBlock = new TransformBlock<DownloadResult, DownloadResult>(result =>
        {
            if (result.Success && !string.IsNullOrEmpty(result.Content))
            {
                result.Title = ExtractTitle(result.Content);
            }
            else
            {
                result.Title = "No Title (due to download failure)";
            }
            progressReporter.Report(result);
            return result;
        }, options);

        var finalBlock = new ActionBlock<DownloadResult>(result =>
        {
            progressReporter.Report(result);

            // Safely collect results
            lock (resultsLock)
            {
                results.Add(result);
            }
        }, options);

        downloadBlock.LinkTo(parseBlock, new DataflowLinkOptions { PropagateCompletion = true });
        parseBlock.LinkTo(finalBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await downloadBlock.SendAsync(url, cancellationToken);
        }

        downloadBlock.Complete();
        await finalBlock.Completion;

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
