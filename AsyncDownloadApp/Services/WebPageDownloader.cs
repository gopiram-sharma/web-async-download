using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

namespace AsyncDownload.Services;

public class WebPageDownloader
{
    private readonly HttpClient _httpClient;
    private readonly DownloaderConfig _config;

    public WebPageDownloader(HttpClient httpClient, DownloaderConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<DownloadResult>> DownloadPagesAsync(IEnumerable<string> urls, IProgressReporter progressReporter)
    {
        var downloadTasks = urls.Select(url => DownloadSinglePageAsync(url, progressReporter)).ToList();
        return (await Task.WhenAll(downloadTasks)).ToList();
    }

    private async Task<DownloadResult> DownloadSinglePageAsync(string url, IProgressReporter progressReporter)
    {
        //TODO: write download code here
        var result = new DownloadResult { Url = url };
        // result.Success = true;
        // result.Title = "Test Page";
        await Task.CompletedTask;
        return result;
    }
}