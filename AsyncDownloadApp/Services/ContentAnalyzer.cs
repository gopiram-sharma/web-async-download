using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AsyncDownload.Models;

namespace AsyncDownload.Services;

public class ContentAnalyzer
{
    public List<PageStatistics> AnalyzePagesInParallel(List<DownloadResult> successfulPages)
    {
        var statistics = new ConcurrentBag<PageStatistics>();
        Parallel.ForEach(successfulPages, page =>
        {
            var pageStats = new PageStatistics { Url = page.Url };
            if (!string.IsNullOrEmpty(page.Content))
            {
                var textOnly = Regex.Replace(page.Content, "<.*?>", " "); // Remove HTML tags
                pageStats.WordCount = Regex.Matches(textOnly, @"\b\w+\b").Count;
                pageStats.ImageCount = Regex.Matches(page.Content, @"<img").Count;
                pageStats.LinkCount = Regex.Matches(page.Content, @"<a\s+href").Count;
            }
            statistics.Add(pageStats);
        });
        return statistics.OrderBy(s => s.Url).ToList();
    }
}