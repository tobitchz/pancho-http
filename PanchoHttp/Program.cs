using System.Text.Json;
using PanchoHttp.Http;

string configPath = Path.Combine(
    AppContext.BaseDirectory,
    "appsettings.json"
);

string json = await File.ReadAllTextAsync(configPath);

using JsonDocument config = JsonDocument.Parse(json);

int port = config.RootElement.GetProperty("Port").GetInt32();

if (port is < 1 or > 65535)
    throw new InvalidOperationException("Port debe estar entre 1 y 65535.");

string webRootSetting = config.RootElement.GetProperty("WebRoot").GetString()
    ?? throw new InvalidOperationException("WebRoot no puede ser nulo.");
if (string.IsNullOrWhiteSpace(webRootSetting))
    throw new InvalidOperationException("WebRoot no puede estar vacío.");
string webRoot = Path.GetFullPath(webRootSetting, AppContext.BaseDirectory);
if (!Directory.Exists(webRoot))
    throw new DirectoryNotFoundException($"No existe el directorio web: {webRoot}");

Console.WriteLine($"Puerto configurado: {port}");
Console.WriteLine($"Directorio web: {webRoot}");

var server = new HttpService(port, webRoot);

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    shutdown.Cancel();
};
await server.StartAsync(shutdown.Token);
