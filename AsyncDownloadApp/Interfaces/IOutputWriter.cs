using System;

namespace AsyncDownload.Interfaces;

/// <summary>
/// Defines an abstraction for writing output, allowing for different implementations
/// (e.g., Console, file, in-memory for testing).
/// </summary>
public interface IOutputWriter
{
    void WriteLine(string message);
    void SetForegroundColor(ConsoleColor color);
    void ResetColor();
}