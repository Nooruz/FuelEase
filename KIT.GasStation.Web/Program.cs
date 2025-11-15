using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Web.Hubs;
using KIT.GasStation.Web.Services;
using KIT.GasStation.Web.Services.Api;
using KIT.GasStation.Web.HostBuilders;
using Serilog;

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "log-.txt"),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7 // Храним только последние 7 дней
    )
    .CreateLogger();

try
{
    Log.Information("Starting up");

var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "KIT.GasStation.Web";
    });

    builder.Host.UseSerilog()
        .AddConfiguration()
        .AddDbContext();

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IGroupRegistry, GroupRegistry>();

    // если клиент (RMK) будет подключаться с другого origin:
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true))); // dev: разрешаем все origin

    builder.Services.AddHttpClient("GasStationApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]);
    });

    builder.Services.AddScoped(typeof(IDataService<>), serviceProvider =>
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return new ApiDataService<DomainObject>(
            httpClientFactory.CreateClient("GasStationApi"),
            resourcePath: /* подставьте имя ресурса для конкретного T через фабрику */ "");
    });

    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        // Подтягиваем конфиг из "Kestrel" секции appsettings.*.json
        options.Configure(context.Configuration.GetSection("Kestrel"));
    });

    var app = builder.Build();

    //app.MapHub<DeviceHub>("/deviceHub");

    app.UseSerilogRequestLogging();
    app.UseCors(); // если включал CORS выше


    app.MapHub<DeviceResponseHub>("/deviceresponse");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed"); // Добавлено логирование ошибки  
}
finally
{
    Log.CloseAndFlush(); // Закрытие логгера  
}





