using System.Text;

namespace PanchoHttp.Http;

public static class RequestLogger
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task LogAsync(string ip, HttpRequest? request, int status,
        string? error = null)
    {
        string logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        DateTime now = DateTime.Now;
        string filePath = Path.Combine(logsDirectory, $"{now:yyyy-MM-dd}.log");
        string entry = $"[{now:yyyy-MM-dd HH:mm:ss}] " +
            $"IP={Escape(ip)} METHOD={Escape(request?.Method ?? "-")} " +
            $"URL={Escape(request?.Url ?? "-")} QUERY={Escape(request?.QueryParams ?? "-")} " +
            $"BODY={Escape(request?.Body ?? "-")} STATUS={status} ERROR={Escape(error ?? "-")}" +
            Environment.NewLine;

        await Gate.WaitAsync();
        try { await File.AppendAllTextAsync(filePath, entry, Encoding.UTF8); }
        finally { Gate.Release(); }
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n")
        .Replace("\t", "\\t");
}
