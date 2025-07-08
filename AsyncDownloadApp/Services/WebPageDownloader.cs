using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

namespace AsyncDownload.Services;

public class WebPageDownloader
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly DownloaderConfig _config;

    public WebPageDownloader(HttpClient httpClient, DownloaderConfig config)
    {
        if (config.MaxConcurrentDownloads <= 0)
            throw new ArgumentException("MaxConcurrentDownloads must be positive.");
        if (config.TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be positive.");
        if (config.MaxContentBytes <= 0)
            throw new ArgumentException("MaxContentBytes must be positive.");

        _httpClient = httpClient;
        _config = config;
        _semaphore = new SemaphoreSlim(_config.MaxConcurrentDownloads, _config.MaxConcurrentDownloads);
    }

    public async Task<List<DownloadResult>> DownloadPagesAsync(IEnumerable<string> urls, IProgressReporter progressReporter, CancellationToken cancellationToken = default)
    {
        var downloadTasks = urls.Select(url => DownloadSinglePageAsync(url, progressReporter, cancellationToken)).ToList();
        return (await Task.WhenAll(downloadTasks)).ToList();
    }

    private async Task<DownloadResult> DownloadSinglePageAsync(string url, IProgressReporter progressReporter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new DownloadResult { Url = url, Success = false, ErrorMessage = "URL is empty or invalid." };

        await _semaphore.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var result = new DownloadResult { Url = url };
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds)).Token
            );
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            // Check Content-Length header if present
            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value > _config.MaxContentBytes)
            {
                result.ErrorMessage = $"Content too large ({response.Content.Headers.ContentLength.Value} bytes).";
                return result;
            }

            // Read up to MaxContentBytes from the stream
            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            int totalRead = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, _config.MaxContentBytes - totalRead), cts.Token)) > 0)
            {
                ms.Write(buffer, 0, read);
                totalRead += read;
                if (totalRead >= _config.MaxContentBytes)
                {
                    result.ErrorMessage = $"Content exceeded maximum allowed size ({_config.MaxContentBytes} bytes).";
                    return result;
                }
            }
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            result.Content = await reader.ReadToEndAsync();
            result.Success = true;
            result.Title = ExtractTitle(result.Content);
        }
        catch (OperationCanceledException)
        {
            result.ErrorMessage = $"Request timed out or was cancelled after {_config.TimeoutSeconds} seconds.";
        }
        catch (HttpRequestException e)
        {
            result.ErrorMessage = $"HTTP request error: {e.Message}";
        }
        catch (Exception e)
        {
            result.ErrorMessage = $"An unexpected error occurred: {e.Message}";
        }
        finally
        {
            _semaphore.Release();
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            progressReporter?.Report(result);
        }
        return result;
    }

    private static string ExtractTitle(string html)
    {
        if (string.IsNullOrEmpty(html)) return "No Title Found";
        var match = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : "No Title Found";
    }
}