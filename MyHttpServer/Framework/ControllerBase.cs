using System.Net;

namespace MyHttpServer.Framework;

public abstract class ControllerBase
{
    private HttpListenerContext? _context;

    protected HttpListenerContext Context =>
        _context ?? throw new InvalidOperationException(
            "Контекст не установлен для контроллера."
        );

    public void SetContext(HttpListenerContext context)
    {
        _context = context;
    }
}