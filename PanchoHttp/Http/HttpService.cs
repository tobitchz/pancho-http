
using System.Net;
using System.Net.Sockets;

namespace PanchoHttp.Http;

public class HttpService
{
    private readonly TcpListener _listener;

    public HttpService(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        _listener.Start();

        Console.WriteLine("Pancho HTTP iniciado");
        Console.WriteLine($"Escuchando en {_listener.LocalEndpoint}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener
                    .AcceptTcpClientAsync(cancellationToken);

                _ = HandleClientAsync(client);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Servidor detenido");
        }
        finally
        {
            _listener.Stop();
        }
    }

    

private async Task HandleClientAsync(TcpClient client)
{
    using (client)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(10)
        );

        string ip = client.Client.RemoteEndPoint?
            .ToString() ?? "Desconocida";

        try
        {
            Console.WriteLine($"Conexion desde {ip}");

            await RequestLogger.LogAsync(
                ip,
                "Conexion recibida"
            );

            NetworkStream stream = client.GetStream();

            await Task.Delay(100, timeout.Token);

            await RequestLogger.LogAsync(
                ip,
                "Conexion procesada"
            );

            Console.WriteLine("Conexion procesada");
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            await RequestLogger.LogAsync(
                ip,
                "Tiempo de espera agotado"
            );
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error procesando cliente: {ex.Message}"
            );
        }
    }
}


}
