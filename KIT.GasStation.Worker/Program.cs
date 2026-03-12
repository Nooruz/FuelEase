using KIT.App.Infrastructure.HostBuilders;
using KIT.GasStation.Web;
using Serilog;

var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30 // Р ТђРЎР‚Р В°Р Р…Р С‘Р С РЎвЂљР С•Р В»РЎРЉР С”Р С• Р С—Р С•РЎРѓР В»Р ВµР Т‘Р Р…Р С‘Р Вµ 7 Р Т‘Р Р…Р ВµР в„–
    )
    .CreateLogger();

try
{
    Log.Information("Starting up");

    var builder = Host.CreateDefaultBuilder(args) // Р ВР В·Р СР ВµР Р…Р ВµР Р…Р С• Р Р…Р В° CreateDefaultBuilder Р Т‘Р В»РЎРЏ Р С—Р С•Р В»РЎС“РЎвЂЎР ВµР Р…Р С‘РЎРЏ IHostBuilder
        .UseSerilog()
        .UseWindowsService(options =>
        {
            options.ServiceName = "KIT.GasStation.Worker";
        })
        .AddHubs()
        .AddFuelDispenserServices()
        .AddHardwareConfigurationsServices()
        .AddCashRegisters()
        .ConfigureServices((hostContext, services) =>
        {
            //var cfg = hostContext.Configuration;
            //var baseUrl = cfg["SignalR:BaseUrl"] ?? "http://localhost:5005";
            //var hubPath = cfg["SignalR:HubPath"] ?? "/deviceHub";
            //var hubUrl = new Uri(new Uri(baseUrl), hubPath).ToString();

            //services.AddTransient(sp =>
            //    new HubConnectionBuilder()
            //        .WithUrl(hubUrl)
            //        .WithAutomaticReconnect()
            //        .Build());
            //services.AddSignalR();

            //services.AddTransient<IHubClient, HubClient>();

            //services.AddSingleton(sp =>
            //{
            //    var cfg = sp.GetRequiredService<IConfiguration>();
            //    var baseUrl = cfg["SignalR:BaseUrl"] ?? "http://localhost:5005";
            //    var hubPath = cfg["SignalR:HubPath"] ?? "/deviceHub";
            //    var hubUrl = new Uri(new Uri(baseUrl), hubPath).ToString();

            //    return new HubConnectionBuilder()
            //        .WithUrl(hubUrl)
            //        .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)])
            //        .Build();
            //});

            //services.AddSingleton<IHubClient, HubClient>();

            services.AddHostedService<Worker>();
        });

    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
    {
        Log.Fatal(e.ExceptionObject as Exception, "Необработанное исключение");
    };

    TaskScheduler.UnobservedTaskException += (sender, e) =>
    {
        Log.Fatal(e.Exception, "Необработанное исключение в задаче");
        e.SetObserved();
    };

    var host = builder.Build(); // Р СџР С•РЎРѓРЎвЂљРЎР‚Р С•Р ВµР Р…Р С‘Р Вµ РЎвЂ¦Р С•РЎРѓРЎвЂљР В°
    await host.RunAsync(); // Р вЂ”Р В°Р С—РЎС“РЎРѓР С” РЎвЂ¦Р С•РЎРѓРЎвЂљР В°
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed"); // Р вЂќР С•Р В±Р В°Р Р†Р В»Р ВµР Р…Р С• Р В»Р С•Р С–Р С‘РЎР‚Р С•Р Р†Р В°Р Р…Р С‘Р Вµ Р С•РЎв‚¬Р С‘Р В±Р С”Р С‘  
    throw;
}
finally
{
    Log.CloseAndFlush(); // Р вЂ”Р В°Р С”РЎР‚РЎвЂ№РЎвЂљР С‘Р Вµ Р В»Р С•Р С–Р С–Р ВµРЎР‚Р В°  
}


