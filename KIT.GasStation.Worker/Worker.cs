using KIT.GasStation.Common.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;

namespace KIT.GasStation.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IFuelDispenserFactory _fuelDispenserFactory;

        public Worker(ILogger<Worker> logger,
            IHardwareConfigurationService hardwareConfigurationService,
            IFuelDispenserFactory fuelDispenserFactory)
        {
            _logger = logger;
            _hardwareConfigurationService = hardwareConfigurationService;
            _fuelDispenserFactory = fuelDispenserFactory;
        }

        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1) Читаем конфигурацию
            var controllers = await _hardwareConfigurationService.GetControllersAsync();

            // 2) Для каждого Controller запускаем отдельную задачу
            var tasks = controllers
                .Select(ctrl => RunControllerLoopAsync(ctrl, stoppingToken))
                .ToList();

            // 3) Ждём завершения (оно произойдёт при отмене токена)
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Эта функция запускает цикл обработки для каждого ТРК.
        /// </summary>
        /// <param name="ctrl">ТРК</param>
        /// <param name="token">Токен</param>
        /// <returns></returns>
        private async Task RunControllerLoopAsync(Controller ctrl, CancellationToken token)
        {
            _logger.LogInformation("Запуск обработки ТРК {Id} на порту {Port}", ctrl.Id, ctrl.ComPort);

            var fuelDispenser = _fuelDispenserFactory.Create(ctrl.Type);
        }
    }
}
