using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.IO;

namespace KIT.GasStation.HostBuilders
{
    public static class AddLoggingHostBuilderExtensions
    {
        public static IHostBuilder AddLogging(this IHostBuilder host)
        {
            return host.UseSerilog((hostingContext, loggerConfiguration) =>
            {
                string? connectionString = hostingContext.Configuration.GetConnectionString("DefaultConnection");

                // Указываем путь для логов
                string logFolderPath = "Logs";
                string logFilePath = Path.Combine(logFolderPath, "log.txt");

                // Проверяем и создаем папку Logs, если её нет
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                // Настройка логирования
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);

                // Логирование в MSSQL
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _ = loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .WriteTo.MSSqlServer(connectionString,
                        new MSSqlServerSinkOptions
                        {
                            TableName = "LogEvents",
                            AutoCreateSqlTable = true,
                        });
                }

                // Логирование в файл .txt
                loggerConfiguration.WriteTo.File(
                    path: "Logs/log.txt",              // Путь к файлу
                    rollingInterval: RollingInterval.Day, // Создание нового файла каждый день
                    retainedFileCountLimit: 30,        // Хранить файлы за последние 30 дней
                    fileSizeLimitBytes: 10_000_000,    // Максимальный размер файла 10 MB
                    rollOnFileSizeLimit: true,         // Создавать новый файл при достижении лимита
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] {Message}{NewLine}{Exception}" // Шаблон
                );

            });
        }
    }
}
