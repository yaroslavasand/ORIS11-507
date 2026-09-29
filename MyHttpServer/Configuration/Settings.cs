using System.Net;
using System.Text.Json;

namespace MyHttpServer.Configuration;

public sealed class Settings
{
    private static readonly Lazy<Settings> LazyInstance = new(Load);

    public static Settings Instance => LazyInstance.Value;

    public ServerSettings Server { get; init; } = new();

    private static Settings Load()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "settings.json"
        );

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Файл settings.json не найден.",
                filePath
            );
        }

        string json = File.ReadAllText(filePath);

        Settings settings = JsonSerializer.Deserialize<Settings>(json)
            ?? throw new InvalidDataException("Файл settings.json пуст.");

        if (settings.Server is null)
        {
            throw new InvalidDataException(
                "В settings.json отсутствует раздел Server."
            );
        }

        if (!IPAddress.TryParse(settings.Server.Host, out IPAddress? address)
            || !IPAddress.IsLoopback(address)
            || address.AddressFamily !=
               System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidDataException(
                "Для этого проекта Host должен быть локальным IPv4-адресом, например 127.0.0.1."
            );
        }

        if (settings.Server.Port is < 1 or > 65535)
        {
            throw new InvalidDataException(
                "Port должен быть числом от 1 до 65535."
            );
        }

        string path = settings.Server.Path.Trim('/');

        if (path.Length == 0 ||
            path.Split('/').Any(segment =>
                segment.Length == 0 ||
                segment.Any(ch =>
                    !char.IsAsciiLetterOrDigit(ch) &&
                    ch != '-' &&
                    ch != '_')))
        {
            throw new InvalidDataException(
                "Некорректное значение Path в settings.json."
            );
        }

        return settings;
    }
}

public sealed class ServerSettings
{
    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 8888;

    public string Path { get; init; } = "connection/";
}