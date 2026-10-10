using System.Net;

namespace MyHttpServer.Framework.Handlers;

public sealed class NotFoundHandler : Handler
{
    public override Task HandleAsync(HttpListenerContext context)
    {
        return HttpResult.Text("Страница не найдена.", 404)
            .WriteAsync(context.Response);
    }
}