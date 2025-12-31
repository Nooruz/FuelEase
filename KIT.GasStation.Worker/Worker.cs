using KIT.GasStation.Common.Factories;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;

namespace KIT.GasStation.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IFuelDispenserFactory _fuelDispenserFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger,
            IHardwareConfigurationService hardwareConfigurationService,
            IFuelDispenserFactory fuelDispenserFactory,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _hardwareConfigurationService = hardwareConfigurationService;
            _fuelDispenserFactory = fuelDispenserFactory;
            _scopeFactory = scopeFactory;
        }

        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1) Читаем конфигурацию
            var controllers = await _hardwareConfigurationService.GetControllersAsync();
            var tasks = new List<Task>();

            foreach (var ctrl in controllers)
            {
                // Берём адреса из конфигурации колонки, исключаем отключённые и дубли
                var addresses = ctrl.Columns
                    .Select(c => c.Address)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToArray();

                foreach (var addr in addresses)
                {
                    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                    var controller = new Controller()
                    {
                        Id = ctrl.Id,
                        Name = ctrl.Name,
                        Type = ctrl.Type,
                        ComPort = ctrl.ComPort,
                        BaudRate = ctrl.BaudRate,
                        Settings = ctrl.Settings,
                        Columns = new(ctrl.Columns.Where(c => c.Address == addr).ToList())
                    };

                    var t = RunControllerLoopAsync(controller, addr, linkedCts.Token);
                    tasks.Add(t);
                }

            }

            try { await Task.WhenAll(tasks); }
            catch (OperationCanceledException) { /* штатная отмена */ }
        }

        /// <summary>
        /// Эта функция запускает цикл обработки для каждого ТРК.
        /// </summary>
        /// <param name="ctrl">ТРК</param>
        /// <param name="token">Токен</param>
        /// <returns></returns>
        private async Task RunControllerLoopAsync(Controller ctrl, int address, CancellationToken token)
        {
            var backoff = TimeSpan.FromSeconds(1);

            var scope = _scopeFactory.CreateAsyncScope();
            var sp = scope.ServiceProvider;

            IFuelDispenserService? service = null;

            try
            {
                _logger.LogInformation("Старт цикла для ТРК {Id} (порт {Port}, тип {Type})",
                ctrl.Id, ctrl.ComPort, ctrl.Type);

                service = _fuelDispenserFactory.Create(sp, ctrl, address);

                // основной цикл сервиса (открытие порта, опрос и т.д.)
                await service.RunAsync(token);

                // Если RunAsync завершился без исключения — выходим (нормальная остановка)
                _logger.LogInformation("ТРК {Id} завершил работу штатно", ctrl.Id);
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Отмена цикла ТРК {Id}", ctrl.Id);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Сбой в цикле ТРК {Id}. Повтор через {Backoff}", ctrl.Id, backoff);
                try
                {
                    await Task.Delay(backoff, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // экспоненциальный бэкофф до 30с
                var next = backoff.TotalSeconds * 2;
                backoff = TimeSpan.FromSeconds(next > 30 ? 30 : next);
            }
            finally
            {
                if (service != null)
                    await service.DisposeAsync();
            }
        }
    }
}
