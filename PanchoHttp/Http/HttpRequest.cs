using System;
using System.Net;

namespace PanchoHttp.Http
{
    public class HttpRequest
    {
        public string Method { get; private set; } = string.Empty;
        public string Url { get; private set; } = string.Empty;
        public string QueryParams { get; private set; } = "-";
        public string Body { get; private set; } = "-";


        public HttpRequest(string rawRequest)
        {
            if (string.IsNullOrWhiteSpace(rawRequest))
                throw new ArgumentException("Solicutud HTTP vacia o mal formada.");

            rawRequest = rawRequest.TrimEnd('\0');

            string[] sections = rawRequest.Split(new[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
            string headerBlock = sections[0];
            string rawBody = sections.Length > 1 ? sections[1] : string.Empty;

            ParseRequestLine(headerBlock);
            ParseBody(rawBody);
        }

        private void ParseRequestLine(string headerBlock)
        {
            string[] lines = headerBlock.Split(new[] { "\r\n" }, StringSplitOptions.None);

            if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
                throw new ArgumentException("Linea de solicitud HTTP vacía.");

            string[] tokens = lines[0].Split(' ');

            if (tokens.Length < 2)
                throw new ArgumentException("Linea de solicitud inválida.");

            Method = tokens[0].ToUpperInvariant();

            string fullUrl = tokens[1];
            string[] urlParts = fullUrl.Split(new[] { '?' }, 2);

            Url = urlParts[0];

            QueryParams = (urlParts.Length > 1 && !string.IsNullOrWhiteSpace(urlParts[1]))
            ? WebUtility.UrlDecode(urlParts[1])
            : "-";

        }

        private void ParseBody(string rawBody)
        {
            if (Method == "POST" && !string.IsNullOrWhiteSpace(rawBody))
            {
                Body = WebUtility.UrlDecode(rawBody.Trim());
            }
            else
            {
                Body = "-";
            }
        }

    }
}