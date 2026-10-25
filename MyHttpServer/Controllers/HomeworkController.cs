using System.Net.Mail;
using MyHttpServer.Framework;
using MyHttpServer.Services;

namespace MyHttpServer.Controllers;

[HttpController("home-work")]
public sealed class HomeworkController : ControllerBase
{
    private readonly EmailService _emailService = new();

    // GET /connection/home-work/send?to=адрес_получателя
    [HttpGET("send")]
    public async Task<HttpResult> Send(string to)
    {
        if (string.IsNullOrWhiteSpace(to) ||
            !MailAddress.TryCreate(to, out var recipient))
        {
            return HttpResult.Text(
                "Некорректная почта получателя.",
                400
            );
        }

        string archivePath =
            Environment.GetEnvironmentVariable("HOMEWORK_ZIP_PATH")
            ?? "";

        try
        {
            Console.WriteLine(
                $"[HomeworkController] Запрос отправки " +
                $"для {recipient.Address}."
            );

            string result = await _emailService.SendHomeworkAsync(
                recipient.Address,
                archivePath
            );

            return HttpResult.Text(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[HomeworkController] Ошибка: {ex.Message}"
            );

            return HttpResult.Text(
                "Не удалось подтвердить отправку. " +
                "Проверьте консоль сервера. " +
                "Если сетевая попытка уже начиналась, " +
                "повторная отправка запрещена до перезапуска.",
                500
            );
        }
    }
}