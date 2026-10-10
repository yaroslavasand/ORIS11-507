using System.Diagnostics;
using MyHttpServer.Configuration;
using MyHttpServer.Core;

try
{
    Settings settings = Settings.Instance;

    using var server = new HttpServer(settings.Server);

    Task serverTask = server.StartAsync();

    
    if (serverTask.IsCompleted)
    {
        await serverTask;
    }
    else
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = server.Address,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Не удалось открыть браузер: {ex.Message}"
            );

            Console.WriteLine(
                $"Откройте адрес вручную: {server.Address}"
            );
        }

        while (!serverTask.IsCompleted)
        {
            string? command = Console.ReadLine();

            if (command is null ||
                command.Trim().Equals(
                    "stop",
                    StringComparison.OrdinalIgnoreCase))
            {
                server.Stop();
                break;
            }

            Console.WriteLine(
                "Введите stop для остановки сервера."
            );
        }

        await serverTask;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.GetBaseException().Message}");
}