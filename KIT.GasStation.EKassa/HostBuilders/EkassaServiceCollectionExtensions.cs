using System;
using System.Net.Http;
using KIT.GasStation.EKassa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KIT.GasStation.EKassa.HostBuilders
{
    public static class EkassaServiceCollectionExtensions
    {
        public static IServiceCollection AddEkassa(this IServiceCollection services, Action<EkassaOptions> configure)
        {
            var opt = new EkassaOptions();
            configure(opt);

            services.AddSingleton(opt);
            services.AddSingleton<EkassaTokenStore>();

            // HttpClient ДЛЯ логина (без auth handler)
            services.AddHttpClient<EkassaLoginApi>(c =>
            {
                c.BaseAddress = opt.BaseUri;
                c.Timeout = opt.Timeout;
            });

            // Auth handler
            services.AddTransient<EkassaAuthHandler>();

            // HttpClient ДЛЯ всего остального (с auth handler)
            services.AddHttpClient<IEkassaClient, EkassaClient>(c =>
            {
                c.BaseAddress = opt.BaseUri;
                c.Timeout = opt.Timeout;
            })
            .AddHttpMessageHandler<EkassaAuthHandler>();

            return services;
        }
    }
}
