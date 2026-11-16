using System.IO.Compression;
using System.Net;
using System.Text;

namespace PanchoHttp.Http;

public static class HttpResponse
{
    public static byte[] Create(int status, byte[] body, string contentType, bool gzip)
    {
        if (gzip && body.Length > 0)
        {
            using var output = new MemoryStream();
            using (var compressor = new GZipStream(output, CompressionLevel.Fastest, true))
                compressor.Write(body);
            body = output.ToArray();
        }

        string headers = $"HTTP/1.1 {status} {(HttpStatusCode)status}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            (gzip && body.Length > 0 ? "Content-Encoding: gzip\r\n" : "") +
            "Connection: close\r\n\r\n";
        byte[] prefix = Encoding.ASCII.GetBytes(headers);
        byte[] response = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, response, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, response, prefix.Length, body.Length);
        return response;
    }
}
