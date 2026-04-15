using KIT.GasStation.Licensing.Client;
using KIT.GasStation.Licensing.Core;
using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Protection;
using KIT.GasStation.Licensing.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KIT.GasStation.Licensing.Extensions;

/// <summary>
/// Расширения для регистрации модуля лицензирования в DI-контейнере.
/// </summary>
public static class LicensingServiceExtensions
{
    /// <summary>
    /// Регистрирует все сервисы модуля лицензирования.
    /// </summary>
    public static IServiceCollection AddLicensing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Конфигурация
        services.Configure<LicensingOptions>(configuration.GetSection(LicensingOptions.SectionName));

        // Core
        services.AddSingleton<LicenseValidator>();
        services.AddSingleton<LicenseStore>();

        // Protection
        services.AddSingleton<TimeGuard>();
        services.AddSingleton<CloneDetector>();
        services.AddSingleton<AntiTamper>();
        services.AddSingleton<SecurityLogger>();

        // HTTP client для онлайн-проверок
        services.AddHttpClient<OnlineLicenseClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "KIT-AZS-License-Client/1.0");
        });

        // Фоновый сервис проверки
        services.AddHostedService<LicenseGuardService>();

        // Регистрируем LicenseGuardService как Singleton для доступа к IsLicenseValid
        services.AddSingleton(provider =>
            provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
                .OfType<LicenseGuardService>()
                .First());

        return services;
    }
}
