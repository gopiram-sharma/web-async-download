using System;

namespace AsyncDownload.Models;

/// <summary>
/// Represents the result of a single web page download attempt.
/// </summary>
public class DownloadResult
{
    public string Url { get; set; }
    public bool Success { get; set; }
    public string Title { get; set; }
    public string Content { get; set; } // It is defined here
    public string ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}