using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

namespace PanchoHttp.Http;

public sealed class HttpService
{
    private readonly TcpListener _listener;
    private readonly string _webRoot;
    private readonly HashSet<Task> _clients = [];
    private readonly object _clientsLock = new();

    public HttpService(int port, string webRoot)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _webRoot = Path.GetFullPath(webRoot);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        Console.WriteLine($"Pancho HTTP escuchando en {_listener.LocalEndpoint}");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                Task task = HandleClientAsync(client, cancellationToken);
                lock (_clientsLock) _clients.Add(task);
                _ = task.ContinueWith(completed =>
                {
                    lock (_clientsLock) _clients.Remove(completed);
                }, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        finally
        {
            _listener.Stop();
            Task[] pending;
            lock (_clientsLock) pending = _clients.ToArray();
            await Task.WhenAll(pending);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken shutdown)
    {
        using (client)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            CancellationToken token = timeout.Token;
            string ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "desconocida";
            HttpRequest? request = null;
            int status = 500;
            try
            {
                NetworkStream stream = client.GetStream();
                request = await HttpRequest.ReadAsync(stream, token);
                if (request is null) return;

                byte[] body;
                string contentType;
                if (request.Method == "POST")
                {
                    status = 200;
                    body = "Solicitud recibida y registrada.\n"u8.ToArray();
                    contentType = "text/plain; charset=utf-8";
                }
                else if (request.Method == "GET" && request.Url == "/health")
                {
                    (status, body, contentType) = await GetFileAsync("/health.html", token);
                }
                else if (request.Method == "GET")
                {
                    (status, body, contentType) = await GetFileAsync(request.Url, token);
                }
                else
                {
                    status = 405;
                    body = "Método no permitido.\n"u8.ToArray();
                    contentType = "text/plain; charset=utf-8";
                }

                bool gzip = AcceptsGzip(request.Headers);
                byte[] response = HttpResponse.Create(status, body, contentType, gzip);
                await RequestLogger.LogAsync(ip, request, status);
                string queryInfo = request.QueryParams.Length > 0
                    ? $" query={ConsoleValue(request.QueryParams)}"
                    : "";
                string bodyInfo = request.Method == "POST"
                    ? $" body={ConsoleValue(request.Body)}"
                    : "";
                Console.WriteLine($"{ip} {request.Method} {request.Url} -> {status}{queryInfo}{bodyInfo}");
                await stream.WriteAsync(response, token);
            }
            catch (HttpProtocolException ex)
            {
                status = ex.Status;
                await LogErrorAsync(ip, request, ex.Message, status);
                await TryWriteErrorAsync(client, status, ex.Message);
            }
            catch (OperationCanceledException) when (!shutdown.IsCancellationRequested)
            {
                await LogErrorAsync(ip, request, "Tiempo de espera agotado", 408);
                await TryWriteErrorAsync(client, 408, "Tiempo de espera agotado");
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error con {ip}: {ex}");
                await LogErrorAsync(ip, request, ex.Message, 500);
                await TryWriteErrorAsync(client, 500, "Error interno");
            }
        }
    }

    private async Task<(int Status, byte[] Body, string ContentType)> GetFileAsync(
        string url, CancellationToken token)
    {
        string relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (url.EndsWith('/') || relative.Length == 0)
            relative = Path.Combine(relative, "index.html");
        string path = Path.GetFullPath(Path.Combine(_webRoot, relative));
        string rootPrefix = _webRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.Ordinal) || url.Contains('\0'))
            throw new HttpProtocolException(400, "Ruta inválida");

        if (Directory.Exists(path))
            path = Path.Combine(path, "index.html");

        if (!File.Exists(path))
        {
            string errorPage = Path.Combine(_webRoot, "error404", "404.html");
            byte[] error = File.Exists(errorPage)
                ? await File.ReadAllBytesAsync(errorPage, token)
                : "Archivo no encontrado.\n"u8.ToArray();
            return (404, error, "text/html; charset=utf-8");
        }

        string type = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream"
        };
        return (200, await File.ReadAllBytesAsync(path, token), type);
    }

    private static bool AcceptsGzip(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Accept-Encoding", out string? value)) return false;
        foreach (string part in value.Split(','))
        {
            string[] pieces = part.Trim().Split(';', StringSplitOptions.TrimEntries);
            if (!pieces[0].Equals("gzip", StringComparison.OrdinalIgnoreCase)) continue;
            string? quality = pieces.Skip(1).FirstOrDefault(x => x.StartsWith("q=", StringComparison.OrdinalIgnoreCase));
            return quality is null ||
                (double.TryParse(quality[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out double q)
                    && q > 0);
        }
        return false;
    }

    private static async Task LogErrorAsync(string ip, HttpRequest? request, string message, int status)
    {
        try { await RequestLogger.LogAsync(ip, request, status, message); }
        catch (Exception ex) { Console.Error.WriteLine($"No se pudo escribir el log: {ex.Message}"); }
    }

    private static string ConsoleValue(string value) => value
        .Replace("\r", "\\r")
        .Replace("\n", "\\n");

    private static async Task TryWriteErrorAsync(TcpClient client, int status, string message)
    {
        try
        {
            byte[] response = HttpResponse.Create(status, Encoding.UTF8.GetBytes(message + "\n"),
                "text/plain; charset=utf-8", false);
            await client.GetStream().WriteAsync(response);
        }
        catch (Exception) { }
    }
}
