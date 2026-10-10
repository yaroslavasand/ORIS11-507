using System.Net;
using System.Reflection;
using MyHttpServer.Configuration;
using MyHttpServer.Framework;
using MyHttpServer.Framework.Handlers;

namespace MyHttpServer.Core;

public sealed class HttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Handler _chain;

    public string Address { get; }

    public HttpServer(ServerSettings settings)
    {
        string basePath = "/" + settings.Path.Trim('/') + "/";

        Address = $"http://{settings.Host}:{settings.Port}{basePath}";

        string staticDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "static"
        );

        var staticHandler = new StaticFileHandler(
            staticDirectory,
            basePath,
            "login-page/login.html"
        );

        var controllerHandler = new ControllerHandler(
            Assembly.GetExecutingAssembly(),
            basePath
        );

        var notFoundHandler = new NotFoundHandler();

        // Цепочка собирается один раз.
        staticHandler
            .SetNext(controllerHandler)
            .SetNext(notFoundHandler);

        _chain = staticHandler;

        _listener.Prefixes.Add(Address);
    }

    public async Task StartAsync()
    {
        _listener.Start();

        Console.WriteLine($"Сервер запущен: {Address}");
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

                Console.WriteLine(
                    $"{context.Request.HttpMethod} " +
                    context.Request.RawUrl
                );

                await SendResponseAsync(context);
            }
        }
        finally
        {
            Console.WriteLine("Сервер остановлен.");
        }
    }

    private async Task SendResponseAsync(
        HttpListenerContext context)
    {
        try
        {
            await _chain.HandleAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Ошибка запроса: {ex.GetBaseException().Message}"
            );

            try
            {
                await HttpResult.Text(
                    "Внутренняя ошибка сервера.",
                    500
                ).WriteAsync(context.Response);
            }
            catch
            {
                // Если соединение закрыто или ответ уже начат,
                // отправить новый ответ может быть невозможно.
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
                // Ответ уже мог закрыться.
            }
        }
    }

    public void Stop()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
        }
    }

    public void Dispose()
    {
        Stop();
        _listener.Close();
    }
}