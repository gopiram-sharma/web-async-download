using System;

namespace AsyncDownload.Models
{
    ///
    /// Represents the configuration settings for the web page downloader.
    /// 
    public class DownloaderConfig
    {
        public int MaxConcurrentDownloads { get; set; } = 3; // Default value if not in config
        public int TimeoutSeconds { get; set; } = 5;       // Default value if not in config
        public int MaxContentBytes { get; set; } = 2 * 1024 * 1024;       // Default value if not in config
        public int MaxRetries { get; set; } = 1;       // Default value if not in config
    }
}