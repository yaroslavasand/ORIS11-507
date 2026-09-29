using MyHttpServer.Configuration;
using MyHttpServer.Core;

try
{
    Settings settings = Settings.Instance;

    using var server = new HttpServer(settings.Server);

    Task serverTask = server.StartAsync();

    while (!serverTask.IsCompleted)
    {
        string? command = Console.ReadLine();

        if (command is null ||
            command.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase))
        {
            server.Stop();
            break;
        }

        Console.WriteLine("Введите stop для остановки сервера.");
    }

    await serverTask;
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}