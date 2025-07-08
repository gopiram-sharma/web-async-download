using System;
using System.Collections.Generic;
using System.Linq;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

namespace AsyncDownload.Reporting;

/// <summary>
/// Final report class to summarize download and analysis results.
/// It is decoupled from the Console and writes to any IOutputWriter.
/// </summary>
public class FinalReport
{
    private readonly IOutputWriter _writer;

    public FinalReport(IOutputWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void PrintDownloadSummary(List<DownloadResult> results)
    {
        _writer.WriteLine(new string('-', 40));
        _writer.WriteLine("Download Phase Complete. Summary:");
        var successfulDownloads = results.Count(r => r.Success);
        var failedDownloads = results.Count(r => !r.Success);
        _writer.WriteLine($"Total URLs: {results.Count}");
        _writer.SetForegroundColor(ConsoleColor.Green);
        _writer.WriteLine($"Successful: {successfulDownloads}");
        _writer.SetForegroundColor(ConsoleColor.Red);
        _writer.WriteLine($"Failed:     {failedDownloads}");
        _writer.ResetColor();
        // if (failedDownloads > 0)
        // {
        //     _writer.WriteLine("\n--- Failed URLs ---");
        //     foreach (var result in results.Where(r => !r.Success))
        //     {
        //         _writer.WriteLine($"- {result.Url}: {result.ErrorMessage}");
        //     }
        // }
    }

    public void PrintAnalysisSummary(List<PageStatistics> stats)
    {
        _writer.WriteLine("\n--- Page Statistics ---");
        _writer.WriteLine($"{"URL".PadRight(80)} {"Word Count",-15} {"Image Count",-15} {"Link Count",-15}");
        _writer.WriteLine(new string('-', 125));
        foreach (var stat in stats)
        {
            string displayUrl = stat.Url.Length > 78 ? stat.Url.Substring(0, 75) + "..." : stat.Url;
            _writer.WriteLine($"{displayUrl.PadRight(80)} {stat.WordCount,-15} {stat.ImageCount,-15} {stat.LinkCount,-15}");
        }
    }
}