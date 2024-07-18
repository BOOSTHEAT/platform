using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services;

internal class FileLogger : IFileLogger
{
    public readonly string filePath;
    private readonly StreamWriter _writer;
    private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1,1);
    private string filePrefix { get; }
    public const string PREFIX = "Console";
    public const int MAX_LOG_FILES = 9;

    public FileLogger(string userLogPath, string userAppName)
    {
        
        if (string.IsNullOrWhiteSpace(userLogPath))
        {
            throw new ArgumentException("User log path is null or empty.");
        }

        if (!Directory.Exists(userLogPath))
        {
           Directory.CreateDirectory(userLogPath);
        }
        
        userAppName = userAppName.Trim().Replace(" ","");
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        filePrefix = $"{userAppName}{PREFIX}";
        var fileName = $"{filePrefix}_{timestamp}.txt";
        filePath = Path.Combine(userLogPath, fileName);
        RotateLogs(userLogPath);
        _writer = new StreamWriter(filePath, true);
    }

    public async Task WriteAsync(string text)
    {
        await semaphoreSlim.WaitAsync();
        await _writer.WriteLineAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {text}");
        await _writer.FlushAsync();
        semaphoreSlim.Release();
    }

    private void RotateLogs(string directoryPath)
    {
        var logFiles = Directory.GetFiles(directoryPath, $"{filePrefix}_*.txt")
            .Select(file => new FileInfo(file))
            .OrderByDescending(fi => fi.Name)
            .ToList();

        if (logFiles.Count > MAX_LOG_FILES - 1)
        {
            foreach (var file in logFiles.Skip(MAX_LOG_FILES - 1))
            {
                file.Delete();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await semaphoreSlim.WaitAsync();
        await _writer.DisposeAsync();
        semaphoreSlim.Release();
    }
}