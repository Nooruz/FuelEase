using KIT.GasStation.Common.HostBuilders;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.Worker;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30 // Храним только последние 7 дней
    )
    .CreateLogger();

try
{
    Log.Information("Starting up");

    var builder = Host.CreateDefaultBuilder(args) // Изменено на CreateDefaultBuilder для получения IHostBuilder
        .UseSerilog()
        .AddHardwareConfigurationsServices() // Добавление сервисов для работы с оборудованием
        .AddCashRegisters() // Добавление сервисов для работы с кассовыми аппаратами
        .ConfigureServices((hostContext, services) =>
        {
            var cfg = hostContext.Configuration;
            var baseUrl = cfg["SignalR:BaseUrl"] ?? "http://localhost:5000";
            var hubPath = cfg["SignalR:HubPath"] ?? "/deviceHub";
            var hubUrl = new Uri(new Uri(baseUrl), hubPath).ToString();

            // 1) Само соединение — Singleton
            services.AddSingleton(sp =>
                new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build());
            services.AddSignalR();                 // <-- чтобы IHubContext резолвился
            // 2) (опционально) автостарт соединения
            services.AddSingleton<IHubClient, HubClient>();

            services.AddHostedService<Worker>(); // Добавление сервиса Worker в DI контейнер
        });

    var host = builder.Build(); // Построение хоста
    await host.RunAsync(); // Запуск хоста
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed"); // Добавлено логирование ошибки  
    throw;
}
finally
{
    Log.CloseAndFlush(); // Закрытие логгера  
}
