using System.Net;

namespace MyHttpServer.Framework.Handlers;

public sealed class StaticFileHandler : Handler
{
    private readonly string _root;
    private readonly string _basePath;
    private readonly string _defaultDocument;

    public StaticFileHandler(
        string root,
        string basePath,
        string defaultDocument)
    {
        _root = Path.GetFullPath(root);
        _basePath = basePath;
        _defaultDocument = defaultDocument;
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            await HandleNextAsync(context);
            return;
        }

        string urlPath = context.Request.Url?.AbsolutePath ?? "";

        if (!urlPath.StartsWith(_basePath, StringComparison.Ordinal))
        {
            await HandleNextAsync(context);
            return;
        }

        string relativePath;

        try
        {
            relativePath = Uri.UnescapeDataString(
                urlPath[_basePath.Length..]
            );
        }
        catch (UriFormatException)
        {
            await HttpResult.Text("Некорректный адрес.", 400)
                .WriteAsync(context.Response);

            return;
        }

        if (relativePath.Length == 0)
        {
            relativePath = _defaultDocument;
        }
        
        string[] segments = relativePath
            .Replace('\\', '/')
            .Split('/');

        if (segments.Any(segment => segment is "." or ".."))
        {
            await HandleNextAsync(context);
            return;
        }

        string filePath;

        try
        {
            filePath = Path.GetFullPath(
                Path.Combine(
                    _root,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)
                )
            );
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is NotSupportedException ||
            ex is PathTooLongException)
        {
            await HandleNextAsync(context);
            return;
        }

        string rootPrefix =
            Path.TrimEndingDirectorySeparator(_root) +
            Path.DirectorySeparatorChar;

        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!filePath.StartsWith(rootPrefix, comparison))
        {
            await HandleNextAsync(context);
            return;
        }

        
        string currentPath = _root;

        foreach (string segment in segments)
        {
            if (segment.Length == 0)
                continue;

            currentPath = Path.Combine(currentPath, segment);

            if ((File.Exists(currentPath) ||
                 Directory.Exists(currentPath)) &&
                File.GetAttributes(currentPath)
                    .HasFlag(FileAttributes.ReparsePoint))
            {
                await HandleNextAsync(context);
                return;
            }
        }

        if (!File.Exists(filePath))
        {
            await HandleNextAsync(context);
            return;
        }

        HttpResult result = await HttpResult.FileAsync(filePath);
        await result.WriteAsync(context.Response);
    }
}