using KIT.GasStation.Worker;
using KIT.GasStation.Common.HostBuilders;
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
        .AddFuelDispensers() // Добавление сервисов для работы с топливными колонками
        .ConfigureServices((hostContext, services) =>
        {
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
