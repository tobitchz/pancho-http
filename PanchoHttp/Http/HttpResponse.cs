using System.IO.Compression;
using System.Net;
using System.Text;


namespace Response
{
    public class HttpResponse()
    {

        public static async Task<byte[]> respuesta(HttpRequest _request, int status, string aceptEndcoding)
        {
            byte[] bytebody = await volverByte(_request.Body);
            var arrayHeader = new StringBuilder();
            var mensajeCode = (HttpStatusCode)status;
            arrayHeader.Append($"HTTP/1.1 {status} {mensajeCode}\r\n");
            arrayHeader.Append($"Content-Type:{_request.ContentType}\r\n");
            arrayHeader.Append($"Content-Length: {_request.ContentLength}\r\n\r\n");
            if (aceptEndcoding == "gzip")
            {
                 bytebody = await comprimir(_request.Body);
                arrayHeader.Append($"Content-Encoding ncoding: gzip\r\n");
                
            }
            byte[] byteheader =  Encoding.UTF8.GetBytes(arrayHeader.ToString());

            return byteheader.Concat(bytebody).ToArray();
        }

        private static async Task<byte[]> comprimir(Stream body)
        {
            using var memoriabody = new MemoryStream();
            using (var bodyComprimido = new GZipStream(memoriabody, CompressionLevel.SmallestSize, leaveOpen: true)) ;
            {
                await body.CopyToAsync(memoriabody);
            }
            byte[] bytebody = memoriabody.ToArray();
            return bytebody;
        }
        private static async Task<byte[]> volverByte(Stream body)
        {
            using var bytebody = new MemoryStream();

            await body.CopyToAsync(bytebody);

            byte[] bytes = bytebody.ToArray();
            return  bytes;
        }

    }

}
