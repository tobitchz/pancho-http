
using System.Text;

namespace PanchoHttp.Http;

public static class RequestLogger
{
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static async Task LogAsync(
        string ip,
        string message)
    {
        string logsDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "logs"
        );

        Directory.CreateDirectory(logsDirectory);

        string filePath = Path.Combine(
            logsDirectory,
            $"{DateTime.Now:yyyy-MM-dd}.log"
        );

        string entry =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
            $"IP: {ip} | {message}{Environment.NewLine}";

        await _lock.WaitAsync();

        try
        {
            await File.AppendAllTextAsync(
                filePath,
                entry,
                Encoding.UTF8
            );
        }
        finally
        {
            _lock.Release();
        }
    }
}
