using System;

namespace AsyncDownload.Models
{
    ///
    /// Represents the statistics of a web page after analysis.
    /// 
    public class PageStatistics
    {
        public string Url { get; set; }
        public int WordCount { get; set; }
        public int ImageCount { get; set; }
        public int LinkCount { get; set; }
    }
}