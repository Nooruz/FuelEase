using KIT.App.Infrastructure.Factories;
using KIT.App.Infrastructure.Services.Hubs;
using KIT.GasStation.FuelDispenser.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.App.Infrastructure.HostBuilders
{
    public static class AddHubsHostBuilderExtensions
    {
        /// <summary>
        /// Добавление сервисов работы с топливными терминалами
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IHostBuilder AddHubs(this IHostBuilder host)
        {
            return host.ConfigureServices((hostContext, services) =>
            {
                //var cfg = hostContext.Configuration;
                //var baseUrl = cfg["SignalR:BaseUrl"] ?? "http://localhost:5005";
                //var hubPath = cfg["SignalR:HubPath"] ?? "/deviceHub";
                //var hubUrl = new Uri(new Uri(baseUrl), hubPath).ToString();

                // 1) Само соединение — Singleton
                //services.AddTransient(sp =>
                //    new HubConnectionBuilder()
                //        .WithUrl(hubUrl)
                //        .WithAutomaticReconnect()
                //        .Build());

                services.AddSingleton(sp =>
                {
                    var cfg = sp.GetRequiredService<IConfiguration>();
                    var baseUrl = cfg["SignalR:BaseUrl"] ?? "http://localhost:5005";
                    var hubPath = cfg["SignalR:HubPath"] ?? "/deviceHub";
                    var hubUrl = new Uri(new Uri(baseUrl), hubPath).ToString();

                    return new HubConnectionBuilder()
                        .WithUrl(hubUrl)
                        .WithAutomaticReconnect(new[] {
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(2),
                            TimeSpan.FromSeconds(10),
                            TimeSpan.FromSeconds(30)})
                        .Build();
                });

                services.AddSingleton<IHubClient, HubClient>();
                services.AddSingleton<IFuelDispenserRegistry, FuelDispenserRegistry>();
                services.AddSingleton<IHubCommandRouter, HubCommandRouter>();
                services.AddSingleton<IFuelDispenserFactory, FuelDispenserFactory>();
            });
        }
    }
}
