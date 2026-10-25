using System.Threading;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MyHttpServer.Services;

public sealed class EmailService
{
    // Общее состояние на всё время работы программы.
    // 0 — отправка ещё не начиналась.
    // 1 — попытка отправки уже началась.
    private static int _sendStarted;

    public async Task<string> SendHomeworkAsync(
        string recipientEmail,
        string archivePath)
    {
        string senderEmail =
            Environment.GetEnvironmentVariable("HOMEWORK_MAIL_FROM")
            ?? "";

        string appPassword =
            Environment.GetEnvironmentVariable("HOMEWORK_MAIL_PASSWORD")
            ?? "";

        if (!MailboxAddress.TryParse(
                senderEmail,
                out var sender))
        {
            throw new InvalidOperationException(
                "В Rider не задан корректный HOMEWORK_MAIL_FROM."
            );
        }

        if (string.IsNullOrWhiteSpace(appPassword))
        {
            throw new InvalidOperationException(
                "В Rider не задан HOMEWORK_MAIL_PASSWORD."
            );
        }

        if (!MailboxAddress.TryParse(
                recipientEmail,
                out var recipient))
        {
            throw new ArgumentException(
                "Некорректный адрес получателя."
            );
        }

        if (string.IsNullOrWhiteSpace(archivePath) ||
            !Path.IsPathFullyQualified(archivePath))
        {
            throw new InvalidOperationException(
                "HOMEWORK_ZIP_PATH должен содержать полный путь к ZIP."
            );
        }

        if (!Path.GetExtension(archivePath).Equals(
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Нужно указать ZIP-архив с расширением .zip."
            );
        }

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                "ZIP-архив не найден. Проверьте HOMEWORK_ZIP_PATH.",
                archivePath
            );
        }

        using var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                "Сандригайло Ярослава Алексеевна",
                sender.Address
            )
        );

        message.To.Add(recipient);

        message.Subject =
            "Домашняя работа — Сандригайло Ярослава Алексеевна — 11-507";

        var builder = new BodyBuilder
        {
            TextBody = """
                Здравствуйте!

                Сандригайло Ярослава Алексеевна 11-507, отправляю свою домашнюю работу.

                ZIP-архив с проектом прикреплён к письму.
                """
        };

        // Прикрепляем существующий архив.
        // Программа не создаёт ZIP заново.
        builder.Attachments.Add(archivePath);

        message.Body = builder.ToMessageBody();

        // Только один запрос за запуск может начать отправку.
        if (Interlocked.CompareExchange(
                ref _sendStarted,
                1,
                0) != 0)
        {
            return "Попытка отправки уже выполнялась в этом запуске. " +
                   "Повторная отправка заблокирована до перезапуска.";
        }

        using var client = new SmtpClient();

        client.Timeout = 30_000;

        // Ограничиваем общее время сетевых операций.
        using var cancellation =
            new CancellationTokenSource(TimeSpan.FromSeconds(90));

        await client.ConnectAsync(
            "smtp.mail.ru",
            465,
            SecureSocketOptions.SslOnConnect,
            cancellation.Token
        );

        await client.AuthenticateAsync(
            sender.Address,
            appPassword,
            cancellation.Token
        );

        await client.SendAsync(
            message,
            cancellation.Token
        );

        Console.WriteLine(
            $"[EmailService] Письмо принято SMTP-сервером " +
            $"для {recipient.Address}."
        );

        // Если письмо принято, ошибка отключения
        // не должна превращать результат в «не отправлено».
        try
        {
            await client.DisconnectAsync(
                true,
                cancellation.Token
            );
        }
        catch (Exception)
        {
            Console.WriteLine(
                "[EmailService] Ошибка отключения после отправки. " +
                "Письмо уже принято SMTP-сервером."
            );
        }

        return "Письмо с архивом принято почтовым сервером. " +
               "Проверьте почту получателя и папку «Спам».";
    }
}