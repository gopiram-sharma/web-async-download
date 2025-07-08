using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;
using AsyncDownload.Reporting;
using AsyncDownload.Services;
using Microsoft.Extensions.Configuration;

namespace AsyncDownload;

class Program
{
    static async Task Main(string[] args)
    {
        IOutputWriter consoleWriter = new ConsoleWriter();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var downloaderConfig = configuration.GetSection("DownloaderConfig").Get<DownloaderConfig>();

        if (args.Length == 0)
        {
            consoleWriter.WriteLine("Usage: please provide the path to a text file containing URLs to download.");
            return;
        }
        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            consoleWriter.WriteLine($"Error: File not found at '{filePath}'");
            return;
        }
        var urlsToDownload = (await File.ReadAllLinesAsync(filePath)).Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
        if (!urlsToDownload.Any())
        {
            consoleWriter.WriteLine("The specified file is empty or contains only whitespace.");
            return;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            consoleWriter.WriteLine("\nCancellation requested. Attempting to stop downloads...");
            cts.Cancel();
            eventArgs.Cancel = true; // Prevent immediate process termination
        };

        // --- Setup Validation ---
        IUrlValidator urlValidator = new UrlValidator();
        var (validUrls, invalidUrls) = urlValidator.Validate(urlsToDownload);

        if (invalidUrls.Any())
        {
            consoleWriter.SetForegroundColor(ConsoleColor.Yellow);
            consoleWriter.WriteLine("The following URLs are invalid and will be skipped:");
            foreach (var invalid in invalidUrls)
                consoleWriter.WriteLine($"  {invalid}");
            consoleWriter.ResetColor();
        }

        if (!validUrls.Any())
        {
            consoleWriter.WriteLine("No valid URLs to download.");
            return;
        }

        // --- Setup Dependencies ---
        IProgressReporter progressReporter = new ConsoleProgressReporter(consoleWriter, validUrls.Count);
        var finalReport = new FinalReport(consoleWriter);

        // --- Download Phase (Asynchronous I/O) ---
        consoleWriter.WriteLine($"Starting download of {validUrls.Count} URLs from '{Path.GetFileName(filePath)}'...");
        consoleWriter.WriteLine($"Configuration: {downloaderConfig.MaxConcurrentDownloads} concurrent downloads, {downloaderConfig.TimeoutSeconds}s timeout.");
        consoleWriter.WriteLine(new string('-', 40));

        List<DownloadResult> allResults;
        using (var httpClient = new HttpClient())
        {
            var downloader = new WebPageDownloader(httpClient, downloaderConfig);
            try
            {
                allResults = await downloader.DownloadPagesAsync(validUrls, progressReporter, cts.Token);
            }
            catch (OperationCanceledException)
            {
                consoleWriter.WriteLine("Download cancelled by user.");
                return;
            }
        }
        finalReport.PrintDownloadSummary(allResults);

        // --- Analysis Phase (Parallel CPU-Bound) ---
        var successfulPages = allResults.Where(r => r.Success && !string.IsNullOrEmpty(r.Content)).ToList();
        if (successfulPages.Any())
        {
            consoleWriter.WriteLine("\n" + new string('=', 40));
            consoleWriter.WriteLine("Starting Content Analysis (Parallel Processing)...");
            var stopwatch = Stopwatch.StartNew();
            var analyzer = new ContentAnalyzer();
            var analysisResults = analyzer.AnalyzePagesInParallel(successfulPages);
            stopwatch.Stop();
            consoleWriter.WriteLine($"Analysis complete in {stopwatch.ElapsedMilliseconds}ms.");
            finalReport.PrintAnalysisSummary(analysisResults);
        }
    }
}