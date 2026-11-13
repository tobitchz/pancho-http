using System.Text.Json;
using PanchoHttp.Http;

string configPath = Path.Combine(
    AppContext.BaseDirectory,
    "appsettings.json"
);

string json = await File.ReadAllTextAsync(configPath);

using JsonDocument config = JsonDocument.Parse(json);

int port = config.RootElement.GetProperty("Port").GetInt32();

string webRoot = config.RootElement.GetProperty("WebRoot").GetString()
    ?? "wwwroot";

Console.WriteLine($"Puerto configurado: {port}");
Console.WriteLine($"Directorio web: {webRoot}");

var server = new HttpService(port);

await server.StartAsync();