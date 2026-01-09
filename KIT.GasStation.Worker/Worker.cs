using KIT.GasStation.Common.Factories;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
            Log.Information("Worker started");
            // Словарь для отслеживания активных задач по ключу (Controller.Id + Address)
            var activeTasks = new ConcurrentDictionary<string, Task>();
            // Источник токена для отмены всех текущих задач при перезагрузке конфигурации
            var currentCycleCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var controllers = await _hardwareConfigurationService.GetControllersAsync();

                    // 1. Отменяем ВСЕ предыдущие задачи циклов для перезапуска с новой конфигурацией
                    currentCycleCts.Cancel();
                    currentCycleCts.Dispose();
                    // Ждем завершения всех предыдущих задач (безопасно)
                    await Task.WhenAll(activeTasks.Values);
                    activeTasks.Clear();

                    // 2. Создаем новый токен для нового цикла
                    currentCycleCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

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

                            var taskKey = $"{ctrl.Id}_{addr}";

                            var task = RunControllerLoopAsync(controller, addr, currentCycleCts.Token);

                            // Сохраняем задачу для последующего отслеживания
                            activeTasks[taskKey] = task;
                        }

                    }

                    // 4. Ожидаем отмены основного токена (остановки службы) БЕЗ активного цикла.
                    // Перезапуск конфигурации теперь инициируется внешне (например, по таймеру или файлу).
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (TaskCanceledException) { /* Штатная остановка */ }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в основном цикле Worker. Перезапуск через 30 сек.");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            // Финализация: отмена всех задач при остановке службы
            currentCycleCts?.Cancel();
            await Task.WhenAll(activeTasks.Values);
        }

        /// <summary>
        /// Эта функция запускает цикл обработки для каждого ТРК.
        /// </summary>
        /// <param name="ctrl">ТРК</param>
        /// <param name="token">Токен</param>
        /// <returns></returns>
        private async Task RunControllerLoopAsync(Controller ctrl, int address, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                IFuelDispenserService? service = null;

                try
                {
                    _logger.LogInformation("Старт/перезапуск цикла для ТРК {Id}", ctrl.Id);
                    service = _fuelDispenserFactory.Create(sp, ctrl, address);
                    await service.RunAsync(token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Отмена цикла ТРК {Id}", ctrl.Id);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Сбой в цикле ТРК {Id}", ctrl.Id);
                    // Ждем перед перезапуском
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                finally
                {
                    if (service != null)
                        await service.DisposeAsync();
                }
            }
        }
    }
}
