using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncDownload.Models;
using Microsoft.Extensions.Configuration;

namespace AsyncDownload;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var downloaderConfig = configuration.GetSection("DownloaderConfig").Get<DownloaderConfig>();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: please provide the path to a text file containing URLs to download.");
            return;
        }
        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found at '{filePath}'");
            return;
        }
        var urlsToDownload = (await File.ReadAllLinesAsync(filePath)).Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
        if (!urlsToDownload.Any())
        {
            Console.WriteLine("The specified file is empty or contains only whitespace.");
            return;
        }

        // TODO: code here
    }
}