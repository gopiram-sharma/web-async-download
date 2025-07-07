using System;
using AsyncDownload.Models;

namespace AsyncDownload.Interfaces;

public interface IProgressReporter
{
    void Report(DownloadResult result);
}