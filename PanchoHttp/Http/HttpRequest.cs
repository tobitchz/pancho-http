using System.Net;
using System.Text;

namespace PanchoHttp.Http;

public sealed class HttpRequest
{
    private const int MaxHeaderBytes = 16 * 1024;
    private const int MaxBodyBytes = 1024 * 1024;

    public string Method { get; }
    public string Url { get; }
    public string QueryParams { get; }
    public string Body { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }

    private HttpRequest(string method, string url, string queryParams, string body,
        Dictionary<string, string> headers)
    {
        Method = method;
        Url = url;
        QueryParams = queryParams;
        Body = body;
        Headers = headers;
    }

    public static async Task<HttpRequest?> ReadAsync(Stream stream, CancellationToken token)
    {
        using var header = new MemoryStream();
        var oneByte = new byte[1];
        int matched = 0;
        byte[] terminator = "\r\n\r\n"u8.ToArray();

        while (matched < terminator.Length)
        {
            int count = await stream.ReadAsync(oneByte, token);
            if (count == 0)
            {
                if (header.Length == 0) return null;
                throw new HttpProtocolException(400, "Solicitud incompleta");
            }

            header.WriteByte(oneByte[0]);
            if (header.Length > MaxHeaderBytes)
                throw new HttpProtocolException(431, "Encabezados demasiado grandes");
            matched = oneByte[0] == terminator[matched]
                ? matched + 1
                : oneByte[0] == terminator[0] ? 1 : 0;
        }

        string[] lines = Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");
        string[] start = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (start.Length != 3 || !start[2].StartsWith("HTTP/1."))
            throw new HttpProtocolException(400, "Línea de solicitud inválida");

        string target = start[1];
        if (!target.StartsWith('/'))
            throw new HttpProtocolException(400, "Ruta inválida");
        int question = target.IndexOf('?');
        string path;
        try { path = Uri.UnescapeDataString(question < 0 ? target : target[..question]); }
        catch (UriFormatException) { throw new HttpProtocolException(400, "Ruta inválida"); }
        string query = question < 0 ? "" : WebUtility.UrlDecode(target[(question + 1)..]);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1))
        {
            if (line.Length == 0) break;
            int colon = line.IndexOf(':');
            if (colon <= 0) throw new HttpProtocolException(400, "Encabezado inválido");
            string name = line[..colon].Trim();
            if (headers.ContainsKey(name))
                throw new HttpProtocolException(400, "Encabezado duplicado");
            headers[name] = line[(colon + 1)..].Trim();
        }

        if (headers.ContainsKey("Transfer-Encoding"))
            throw new HttpProtocolException(501, "Transfer-Encoding no admitido");
        int length = 0;
        if (headers.TryGetValue("Content-Length", out string? value) &&
            (!int.TryParse(value, out length) || length < 0))
            throw new HttpProtocolException(400, "Content-Length inválido");
        if (length > MaxBodyBytes)
            throw new HttpProtocolException(413, "Cuerpo demasiado grande");

        byte[] body = new byte[length];
        try { await stream.ReadExactlyAsync(body, token); }
        catch (EndOfStreamException) { throw new HttpProtocolException(400, "Cuerpo incompleto"); }

        return new HttpRequest(start[0].ToUpperInvariant(), path, query,
            Encoding.UTF8.GetString(body), headers);
    }
}

public sealed class HttpProtocolException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
