using Microsoft.Extensions.Configuration;
using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace KIT.GasStation.Services
{
    public static class ServiceManager
    {
        private static readonly string WebServiceName;
        private static readonly string WorkerServiceName;
        private static readonly TimeSpan StartTimeout;
        private static readonly TimeSpan StopTimeout;

        static ServiceManager()
        {
            try
            {
                var cfg = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                WebServiceName = cfg["Services:WebName"] ?? "KITWeb";
                WorkerServiceName = cfg["Services:WorkerName"] ?? "KITWorker";
                if (!int.TryParse(cfg["Services:StartTimeoutSeconds"], out var startSec)) startSec = 60;
                if (!int.TryParse(cfg["Services:StopTimeoutSeconds"], out var stopSec)) stopSec = 60;
                StartTimeout = TimeSpan.FromSeconds(startSec);
                StopTimeout = TimeSpan.FromSeconds(stopSec);
            }
            catch
            {
                WebServiceName = "KITWeb";
                WorkerServiceName = "KITWorker";
                StartTimeout = TimeSpan.FromSeconds(60);
                StopTimeout = TimeSpan.FromSeconds(60);
            }
        }

        public static Task StartWebAsync() => StartAsync(WebServiceName, StartTimeout);
        public static Task StopWebAsync() => StopAsync(WebServiceName, StopTimeout);
        public static Task StartWorkerAsync() => StartAsync(WorkerServiceName, StartTimeout);
        public static Task StopWorkerAsync() => StopAsync(WorkerServiceName, StopTimeout);

        public static Task<ServiceControllerStatus> GetWebStatusAsync() => GetStatusAsync(WebServiceName);
        public static Task<ServiceControllerStatus> GetWorkerStatusAsync() => GetStatusAsync(WorkerServiceName);

        private static Task<ServiceControllerStatus> GetStatusAsync(string serviceName) => Task.Run(() =>
        {
            using var sc = new ServiceController(serviceName);
            return sc.Status;
        });

        private static Task StartAsync(string serviceName, TimeSpan timeout) => Task.Run(() =>
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Running) return;
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
        });

        private static async Task StopAsync(string serviceName, TimeSpan timeout)
        {
            await Task.Run(() =>
            {
                using var sc = new ServiceController(serviceName);

                // 1) Получение статуса может выбросить AccessDenied
                var status = sc.Status;

                if (status == ServiceControllerStatus.Stopped)
                    return;

                if (!sc.CanStop)
                    throw new InvalidOperationException($"Service '{serviceName}' cannot be stopped. Current status: {status}");

                sc.Stop();

                sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // 2) ВАЖНО: после WaitForStatus проверяем, успели ли остановиться
                sc.Refresh();
                if (sc.Status != ServiceControllerStatus.Stopped)
                    throw new System.TimeoutException($"Timeout stopping service '{serviceName}'. Current status: {sc.Status}");
            }).ConfigureAwait(false);
        }
    }
}
