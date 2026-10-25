using MyHttpServer.Framework;

namespace MyHttpServer.Controllers;

[HttpController("auth")]
public sealed class AuthController : ControllerBase
{
    // GET /connection/auth/login
    [HttpGET("login")]
    public async Task<HttpResult> LoginPage()
    {
        
        Console.WriteLine(
            $"Открытие формы: {Context.Request.HttpMethod} " +
            Context.Request.RawUrl
        );

        string path = Path.Combine(
            AppContext.BaseDirectory,
            "static",
            "login-page",
            "login.html"
        );

        return await HttpResult.FileAsync(path);
    }

    // POST /connection/auth/login
    [HttpPOST("login")]
    public HttpResult Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return HttpResult.Text(
                "Email и пароль не должны быть пустыми.",
                400
            );
        }


        Console.WriteLine(
            $"Метод запроса: {Context.Request.HttpMethod}"
        );

        Console.WriteLine(
            $"Адрес запроса: {Context.Request.RawUrl}"
        );


        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Пароль: {password}");

        return HttpResult.Html("""
                               <!DOCTYPE html>
                               <html lang="ru">
                               <head>
                                   <meta charset="utf-8">
                                   <title>Данные получены</title>
                               </head>
                               <body>
                                   <h1>Данные получены сервером</h1>
                                   <p>Учебные значения выведены в консоль.</p>
                                   <a href="login">Вернуться к форме</a>
                               </body>
                               </html>
                               """);
    }
}