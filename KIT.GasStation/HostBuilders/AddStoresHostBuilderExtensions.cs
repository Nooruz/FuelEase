using DevExpress.Mvvm.POCO;
using KIT.GasStation.State.Authenticators;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Discounts;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Notifications;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HostBuilders
{
    public static class AddStoresHostBuilderExtensions
    {
        public static IHostBuilder AddStores(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                _ = services.AddSingleton<IUserStore, UserStore>();
                _ = services.AddSingleton<IShiftStore, ShiftStore>();
                _ = services.AddSingleton<INavigator, Navigator>();
                _ = services.AddSingleton<IAuthenticator, Authenticator>();
                
                

                _ = services.AddSingleton<NozzleStore>();
                _ = services.AddSingleton<INozzleStore>(sp => sp.GetRequiredService<NozzleStore>());
                _ = services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<NozzleStore>());

                _ = services.AddSingleton<CashRegisterStore>();
                _ = services.AddSingleton<ICashRegisterStore>(sp => sp.GetRequiredService<CashRegisterStore>());
                _ = services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CashRegisterStore>());

                _ = services.AddSingleton<DisсountStore>();
                _ = services.AddSingleton<IDisсountStore>(sp => sp.GetRequiredService<DisсountStore>());
                _ = services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DisсountStore>());

                _ = services.AddSingleton<INotificationStore, NotificationStore>();
            });
        }
    }
}
