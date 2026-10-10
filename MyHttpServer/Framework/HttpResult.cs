using System.Net;
using System.Text;

namespace MyHttpServer.Framework;

public sealed class HttpResult
{
    private readonly byte[] _body;
    private readonly string _contentType;
    private readonly int _statusCode;

    private HttpResult(
        byte[] body,
        string contentType,
        int statusCode)
    {
        _body = body;
        _contentType = contentType;
        _statusCode = statusCode;
    }

    public static HttpResult Text(string text, int statusCode = 200)
    {
        return new HttpResult(
            Encoding.UTF8.GetBytes(text),
            "text/plain; charset=utf-8",
            statusCode
        );
    }

    public static HttpResult Html(string html, int statusCode = 200)
    {
        return new HttpResult(
            Encoding.UTF8.GetBytes(html),
            "text/html; charset=utf-8",
            statusCode
        );
    }

    public static async Task<HttpResult> FileAsync(string path)
    {
        if (!File.Exists(path))
        {
            return Text("Файл не найден.", 404);
        }

        byte[] body = await File.ReadAllBytesAsync(path);

        string contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };

        return new HttpResult(body, contentType, 200);
    }

    public async Task WriteAsync(HttpListenerResponse response)
    {
        response.StatusCode = _statusCode;
        response.ContentType = _contentType;
        response.ContentLength64 = _body.Length;

        await response.OutputStream.WriteAsync(_body);
    }
}