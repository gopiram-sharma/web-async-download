using System;
using AsyncDownload.Interfaces;

namespace AsyncDownload.Reporting;

/// <summary>
/// A concrete implementation of IOutputWriter that writes to the system console.
/// </summary>
public class ConsoleWriter : IOutputWriter
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void SetForegroundColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }

    public void ResetColor()
    {
        Console.ResetColor();
    }
}