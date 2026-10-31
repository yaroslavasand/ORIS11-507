using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using QuizServer;

const string address = "http://localhost:8080/";

var service = new QuizService();

using var listener = new HttpListener();
listener.Prefixes.Add(address);
listener.Start();

Console.WriteLine($"Сервер запущен: {address}quizzes");
Console.WriteLine("Для остановки нажми Ctrl+C.");

while (true)
{
    var context = await listener.GetContextAsync();

    try
    {
        await HandleRequest(context, service);
    }
    catch (ArgumentException exception)
    {
        await WriteHtml(
            context.Response,
            400,
            HtmlPages.Error(400, exception.Message));
    }
    catch (KeyNotFoundException exception)
    {
        await WriteHtml(
            context.Response,
            404,
            HtmlPages.Error(404, exception.Message));
    }
    catch (Exception exception)
    {
        Console.WriteLine(exception);

        await WriteHtml(
            context.Response,
            500,
            HtmlPages.Error(500, "Внутренняя ошибка сервера."));
    }
    finally
    {
        context.Response.Close();
    }
}

static async Task HandleRequest(
    HttpListenerContext context,
    QuizService service)
{
    var request = context.Request;
    var response = context.Response;

    string path = request.Url?.AbsolutePath ?? "/";
    string method = request.HttpMethod;

    switch ((method, path))
    {
        case ("GET", "/quizzes"):
        {
            int minQuestions = request.QueryString["minQuestions"] is null
                ? 0
                : ReadInteger(request.QueryString, "minQuestions");

            var result = service.GetAll(minQuestions);

            await WriteHtml(
                response,
                200,
                HtmlPages.List(result, minQuestions));

            return;
        }

        case ("GET", "/quizzes/details"):
        {
            int id = ReadId(request.QueryString);
            var quiz = service.GetById(id);

            await WriteHtml(
                response,
                200,
                HtmlPages.Details(quiz));

            return;
        }

        case ("POST", "/quizzes/create"):
        {
            var form = await ReadForm(request);

            int questionsCount = ReadInteger(form, "questionsCount");
            var quiz = service.Create(
                ReadRequired(form, "topic"),
                questionsCount);

            response.StatusCode = 303;
            response.RedirectLocation =
                $"/quizzes/details?id={quiz.Id}";
            response.ContentLength64 = 0;

            return;
        }

        case ("PUT", "/quizzes/update"):
        {
            int id = ReadId(request.QueryString);
            var form = await ReadForm(request);


            string topic = ReadRequired(form, "topic");
            int questionsCount = ReadInteger(form, "questionsCount");
            string status = ReadRequired(form, "status");

            service.Update(id, topic, questionsCount, status);

            response.StatusCode = 204;
            response.ContentLength64 = 0;

            return;
        }

        default:
        {
            await WriteHtml(
                response,
                404,
                HtmlPages.Error(404, "Маршрут не найден."));

            return;
        }
    }
}

static string ReadRequired(NameValueCollection values, string name)
{
    string[]? entries = values.GetValues(name);

    if (entries is null || entries.Length != 1)
    {
        throw new ArgumentException(
            $"Поле {name} должно быть передано ровно один раз.");
    }

    return entries[0];
}

static int ReadInteger(NameValueCollection values, string name)
{
    string text = ReadRequired(values, name);

    if (!int.TryParse(
            text,
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out int value))
    {
        throw new ArgumentException(
            $"Поле {name} должно быть целым числом.");
    }

    return value;
}

static int ReadId(NameValueCollection values)
{
    int id = ReadInteger(values, "id");

    if (id <= 0)
    {
        throw new ArgumentException(
            "Id должен быть положительным целым числом.");
    }

    return id;
}

static async Task<NameValueCollection> ReadForm(
    HttpListenerRequest request)
{
    string contentType = request.ContentType?
        .Split(';', 2)[0]
        .Trim() ?? "";

    if (!contentType.Equals(
            "application/x-www-form-urlencoded",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException(
            "Ожидается Content-Type: application/x-www-form-urlencoded.");
    }

    using var reader = new StreamReader(
        request.InputStream,
        Encoding.UTF8);

    string body = await reader.ReadToEndAsync();

    var result = new NameValueCollection();

    foreach (string pair in body.Split(
                 '&',
                 StringSplitOptions.RemoveEmptyEntries))
    {
        string[] parts = pair.Split('=', 2);

        string key = WebUtility.UrlDecode(parts[0]);
        string value = parts.Length == 2
            ? WebUtility.UrlDecode(parts[1])
            : "";

        result.Add(key, value);
    }

    return result;
}

static async Task WriteHtml(
    HttpListenerResponse response,
    int statusCode,
    string html)
{
    byte[] bytes = Encoding.UTF8.GetBytes(html);

    response.StatusCode = statusCode;
    response.ContentType = "text/html; charset=utf-8";
    response.ContentEncoding = Encoding.UTF8;
    response.ContentLength64 = bytes.Length;

    await response.OutputStream.WriteAsync(bytes);
}