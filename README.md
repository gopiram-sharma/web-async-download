# Async Web Page Downloader

A C# console application for downloading multiple web pages asynchronously with configurable concurrency, timeout, and progress reporting. After downloading, it analyzes the content of successful downloads in parallel.

---

## Features

- **Asynchronous downloads** with configurable maximum concurrency.
- **Timeout** for each download.
- **Graceful cancellation** (Ctrl+C).
- **Progress reporting** to the console.
- **Summary report** of download results.
- **Parallel content analysis** of downloaded pages.

---

## Configuration

Settings are read from `appsettings.json`:

```json
{
  "DownloaderConfig": {
    "MaxConcurrentDownloads": 5,
    "TimeoutSeconds": 10
  }
}
```

- `MaxConcurrentDownloads`: Maximum number of simultaneous downloads.
- `TimeoutSeconds`: Timeout for each download request.

---

## Usage

1. **Prepare a text file** with one URL per line (e.g., `Files/InputUrls.txt`).
2. Go to the AsyncDownloadApp directory and **Run the app** from the command line:

   ```sh
   dotnet run "./Files/InputUrls.txt"
   ```

3. **Cancel downloads** at any time with `Ctrl+C`.

---

## Output

- Progress and results are printed to the console.
- After downloads, a summary is shown.
- If downloads succeed, content analysis is performed and reported.

---

## Requirements

- [.NET 8.0 SDK or later](https://dotnet.microsoft.com/download)
- Internet access for downloads

---

## Example

```
Starting download of 100 URLs from 'InputUrls.txt'...
Configuration: 3 concurrent downloads, 5s timeout.
----------------------------------------
[1/10] Success: https://example.com
[2/10] Success: https://dotnet.microsoft.com
...
Download Phase Complete. Summary:
Total URLs: 100
Successful: 54
Failed:     46

========================================
Starting Content Analysis (Parallel Processing)...
Analysis complete in 4109ms.
```
