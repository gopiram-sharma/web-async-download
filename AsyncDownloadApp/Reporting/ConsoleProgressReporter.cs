using System;
using System.Threading;
using AsyncDownload.Interfaces;
using AsyncDownload.Models;

namespace AsyncDownload.Reporting;

///
/// Progress report class to summarize download results.
/// 
public class ConsoleProgressReporter : IProgressReporter
{
    private readonly IOutputWriter _writer;
    private readonly int _totalCount;
    private int _processedCount = 0;

    public ConsoleProgressReporter(IOutputWriter writer, int totalCount)
    {
        _writer = writer;
        _totalCount = totalCount;
    }

    public void Report(DownloadResult result)
    {
        int currentCount = Interlocked.Increment(ref _processedCount);
        string progressStatus = $"[{currentCount}/{_totalCount}]";

        if (result.Success)
        {
            _writer.SetForegroundColor(ConsoleColor.Green);
            _writer.WriteLine($"{progressStatus} SUCCESS: {result.Url} -> \"{result.Title}\" ({result.DurationMs}ms)");
            _writer.ResetColor();
        }
        else
        {
            _writer.SetForegroundColor(ConsoleColor.Red);
            _writer.WriteLine($"{progressStatus} FAILED:  {result.Url} -> {result.ErrorMessage}");
            _writer.ResetColor();
        }
    }
}