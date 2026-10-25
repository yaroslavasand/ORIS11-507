using System.Globalization;
using System.Net;
using System.Reflection;
using System.Web;

namespace MyHttpServer.Framework.Handlers;

public sealed class ControllerHandler : Handler
{
    private readonly string _basePath;
    private readonly Type[] _controllers;

    public ControllerHandler(Assembly assembly, string basePath)
    {
        _basePath = basePath;
        
        _controllers = assembly.GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.GetCustomAttribute<HttpController>() is not null)
            .ToArray();
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        string path = request.Url?.AbsolutePath ?? "";

        if (!path.StartsWith(_basePath, StringComparison.Ordinal))
        {
            await HandleNextAsync(context);
            return;
        }

        string[] segments = path[_basePath.Length..]
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 2)
        {
            await HandleNextAsync(context);
            return;
        }

        string controllerName = segments[0];
        string route = segments[1];

        Type? controllerType = _controllers.FirstOrDefault(type =>
            string.Equals(
                type.GetCustomAttribute<HttpController>()!.Name,
                controllerName,
                StringComparison.OrdinalIgnoreCase
            ));

        if (controllerType is null)
        {
            await HandleNextAsync(context);
            return;
        }

        MethodInfo? method = controllerType
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            )
            .FirstOrDefault(candidate =>
            {
                string? methodRoute = request.HttpMethod switch
                {
                    "GET" =>
                        candidate.GetCustomAttribute<HttpGET>()?.Route,

                    "POST" =>
                        candidate.GetCustomAttribute<HttpPOST>()?.Route,

                    _ => null
                };

                return string.Equals(
                    methodRoute,
                    route,
                    StringComparison.OrdinalIgnoreCase
                );
            });

        if (method is null)
        {
            await HandleNextAsync(context);
            return;
        }

        var data = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        var query = HttpUtility.ParseQueryString(
            request.Url?.Query ?? ""
        );

        foreach (string? key in query.AllKeys)
        {
            if (key is not null)
            {
                data[key] = query[key] ?? "";
            }
        }

        if (request.HasEntityBody)
        {
            string contentType = request.ContentType?
                .Split(';')[0]
                .Trim() ?? "";

            if (!contentType.Equals(
                    "application/x-www-form-urlencoded",
                    StringComparison.OrdinalIgnoreCase))
            {
                await HttpResult.Text(
                    "Поддерживается только обычная HTML-форма.",
                    415
                ).WriteAsync(context.Response);

                return;
            }

            const int maxBodyBytes = 16_384;

            if (request.ContentLength64 > maxBodyBytes)
            {
                await HttpResult.Text("Слишком большой запрос.", 413)
                    .WriteAsync(context.Response);

                return;
            }

            using var body = new MemoryStream();
            byte[] buffer = new byte[4096];

            while (true)
            {
                int read = await request.InputStream.ReadAsync(buffer);

                if (read == 0)
                    break;

                if (body.Length + read > maxBodyBytes)
                {
                    await HttpResult.Text("Слишком большой запрос.", 413)
                        .WriteAsync(context.Response);

                    return;
                }

                body.Write(buffer, 0, read);
            }

            string formText = request.ContentEncoding.GetString(
                body.ToArray()
            );

            var form = HttpUtility.ParseQueryString(formText);

            foreach (string? key in form.AllKeys)
            {
                if (key is not null)
                {
                    
                    data[key] = form[key] ?? "";
                }
            }
        }

        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            if (!data.TryGetValue(parameter.Name!, out string? value))
            {
                if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                    continue;
                }

                await HttpResult.Text(
                    $"Отсутствует обязательное поле: {parameter.Name}.",
                    400
                ).WriteAsync(context.Response);

                return;
            }

            Type targetType =
                Nullable.GetUnderlyingType(parameter.ParameterType) ??
                parameter.ParameterType;

            try
            {
                arguments[i] = targetType == typeof(string)
                    ? value
                    : Convert.ChangeType(
                        value,
                        targetType,
                        CultureInfo.InvariantCulture
                    );
            }
            catch (Exception ex) when (
                ex is FormatException ||
                ex is OverflowException ||
                ex is InvalidCastException)
            {
                await HttpResult.Text(
                    $"Некорректное значение поля: {parameter.Name}.",
                    400
                ).WriteAsync(context.Response);

                return;
            }
        }
        
        object controller = Activator.CreateInstance(controllerType)
                            ?? throw new InvalidOperationException(
                                "Не удалось создать контроллер."
                            );
        
        if (controller is ControllerBase controllerBase)
        {
            controllerBase.SetContext(context);
        }
        
        object? result = method.Invoke(controller, arguments);

        
        if (result is Task task)
        {
            await task;

            result = task.GetType()
                .GetProperty("Result")?
                .GetValue(task);
        }

        HttpResult responseResult = result switch
        {
            HttpResult httpResult => httpResult,
            string text => HttpResult.Text(text),
            null => HttpResult.Text(""),
            _ => HttpResult.Text(result.ToString() ?? "")
        };

        await responseResult.WriteAsync(context.Response);
    }
}