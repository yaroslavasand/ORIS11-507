using System.Net;

namespace MyHttpServer.Framework.Handlers;

public abstract class Handler
{
    private Handler? _next;

    public Handler SetNext(Handler next)
    {
        _next = next;
        return next;
    }

    public abstract Task HandleAsync(HttpListenerContext context);

    protected Task HandleNextAsync(HttpListenerContext context)
    {
        return _next?.HandleAsync(context) ?? Task.CompletedTask;
    }
}