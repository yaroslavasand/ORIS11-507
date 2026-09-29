using System.Net;
using System.Text;
using MyHttpServer.Configuration;

namespace MyHttpServer.Core;

public sealed class HttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _basePath;
    private readonly string _staticDirectory;
    private readonly string _address;

    private static readonly HashSet<string> AllowedImages =
        new(StringComparer.Ordinal)
        {
            "Frame 1261155601.svg",
            "Frame 1261155602.svg",
            "Frame 1261155604.svg",
            "Frame 1261155605.svg",
            "Frame 1261155606.svg",
            "Frame 1261155607.svg"
        };

    public HttpServer(ServerSettings settings)
    {
        _basePath = "/" + settings.Path.Trim('/') + "/";
        _address = $"http://{settings.Host}:{settings.Port}{_basePath}";

        _staticDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "static"
        );

        _listener.Prefixes.Add(_address);
    }

    public async Task StartAsync()
    {
        string htmlPath = Path.Combine(
            _staticDirectory,
            "search-engine.html"
        );

        if (!File.Exists(htmlPath))
        {
            throw new FileNotFoundException(
                "Файл search-engine.html не найден.",
                htmlPath
            );
        }

        _listener.Start();

        Console.WriteLine($"Сервер запущен: {_address}");
        Console.WriteLine("Для остановки введите stop.");

        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException) when (!_listener.IsListening)
                {
                    break;
                }
                catch (ObjectDisposedException) when (!_listener.IsListening)
                {
                    break;
                }

                try
                {
                    await HandleRequestAsync(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Ошибка обработки запроса: {ex.Message}"
                    );

                    try
                    {
                        await SendTextAsync(
                            context.Response,
                            500,
                            "Внутренняя ошибка сервера."
                        );
                    }
                    catch
                    {
                        // Клиент мог закрыть соединение.
                    }
                }
                finally
                {
                    try
                    {
                        context.Response.Close();
                    }
                    catch
                    {
                        // Соединение уже могло закрыться.
                    }
                }
            }
        }
        finally
        {
            Console.WriteLine("Сервер остановлен.");
        }
    }

    public void Stop()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
        }
    }

    private async Task HandleRequestAsync(
        HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        Console.WriteLine(
            $"{request.HttpMethod} {request.RawUrl}"
        );

        if (request.HttpMethod != "GET")
        {
            response.Headers.Add("Allow", "GET");

            await SendTextAsync(
                response,
                405,
                "Метод не поддерживается."
            );

            return;
        }

        string urlPath = request.Url?.AbsolutePath ?? "";

        if (urlPath == _basePath)
        {
            string htmlPath = Path.Combine(
                _staticDirectory,
                "search-engine.html"
            );

            await SendFileAsync(
                response,
                htmlPath,
                "text/html; charset=utf-8"
            );

            return;
        }

        string imagesPrefix = _basePath + "images/";

        if (urlPath.StartsWith(
                imagesPrefix,
                StringComparison.Ordinal))
        {
            string fileName;

            try
            {
                fileName = Uri.UnescapeDataString(
                    urlPath[imagesPrefix.Length..]
                );
            }
            catch (UriFormatException)
            {
                await SendTextAsync(
                    response,
                    400,
                    "Некорректный адрес."
                );

                return;
            }

            if (AllowedImages.Contains(fileName))
            {
                string imagePath = Path.Combine(
                    _staticDirectory,
                    "images",
                    fileName
                );

                await SendFileAsync(
                    response,
                    imagePath,
                    "image/svg+xml"
                );

                return;
            }
        }

        await SendTextAsync(
            response,
            404,
            "Страница не найдена."
        );
    }

    private static async Task SendFileAsync(
        HttpListenerResponse response,
        string filePath,
        string contentType)
    {
        if (!File.Exists(filePath))
        {
            await SendTextAsync(
                response,
                404,
                "Файл не найден."
            );

            return;
        }

        byte[] data = await File.ReadAllBytesAsync(filePath);

        response.StatusCode = 200;
        response.ContentType = contentType;
        response.ContentLength64 = data.Length;

        await response.OutputStream.WriteAsync(data);
    }

    private static async Task SendTextAsync(
        HttpListenerResponse response,
        int statusCode,
        string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);

        response.StatusCode = statusCode;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = data.Length;

        await response.OutputStream.WriteAsync(data);
    }

    public void Dispose()
    {
        Stop();
        _listener.Close();
    }
}