using KIT.GasStation.Common.Factories;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace KIT.GasStation.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IFuelDispenserFactory _fuelDispenserFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPortManager _portManager;
        private PortLease? _lease;

        public Worker(ILogger<Worker> logger,
            IHardwareConfigurationService hardwareConfigurationService,
            IFuelDispenserFactory fuelDispenserFactory,
            IServiceScopeFactory scopeFactory,
            IPortManager portManager)
        {
            _logger = logger;
            _hardwareConfigurationService = hardwareConfigurationService;
            _fuelDispenserFactory = fuelDispenserFactory;
            _scopeFactory = scopeFactory;
            _portManager = portManager;
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
                        var port = await OpenPort(ctrl, currentCycleCts.Token);

                        switch (ctrl.Type)
                        {
                            case ControllerType.None:
                                break;
                            case ControllerType.Lanfeng:
                                CreateLanfeng(ctrl, activeTasks, currentCycleCts.Token, port);
                                break;
                            case ControllerType.Gilbarco:
                                CreateGilbarco(ctrl, activeTasks, currentCycleCts.Token, port);
                                break;
                            case ControllerType.Emulator:
                                CreateEmulator(ctrl, activeTasks, currentCycleCts.Token);
                                break;
                            case ControllerType.PKElectronics:
                                break;
                            case ControllerType.TechnoProjekt:
                                break;
                            default:
                                break;
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
        private async Task RunControllerLoopAsync(Controller ctrl, 
            int address, 
            CancellationToken token, 
            ISharedSerialPortService port)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                IFuelDispenserService? service = null;

                try
                {
                    _logger.LogInformation("Старт/перезапуск цикла для ТРК {Id}", ctrl.Id);
                    service = _fuelDispenserFactory.Create(sp, ctrl, address, port);
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

        private async Task RunControllerLoopAsync(Controller ctrl,
            CancellationToken token, 
            ISharedSerialPortService port)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                IFuelDispenserService? service = null;

                try
                {
                    _logger.LogInformation("Старт/перезапуск цикла для ТРК {Id}", ctrl.Id);
                    service = _fuelDispenserFactory.Create(sp, ctrl, port);
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

        private async Task RunControllerLoopAsync(Controller ctrl,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateAsyncScope();
                var sp = scope.ServiceProvider;
                IFuelDispenserService? service = null;

                try
                {
                    _logger.LogInformation("Старт/перезапуск цикла для ТРК {Id}", ctrl.Id);
                    service = _fuelDispenserFactory.Create(sp, ctrl);
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

        #region Helpers

        private void CreateLanfeng(Controller ctrl, 
            ConcurrentDictionary<string, Task> activeTasks, 
            CancellationToken token,
            ISharedSerialPortService port)
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

                var task = RunControllerLoopAsync(controller, addr, token, port);

                // Сохраняем задачу для последующего отслеживания
                activeTasks[taskKey] = task;
            }
        }

        private void CreateGilbarco(Controller ctrl, 
            ConcurrentDictionary<string, Task> activeTasks, 
            CancellationToken token,
            ISharedSerialPortService port)
        {
            var taskKey = $"{ctrl.Id}";

            var task = RunControllerLoopAsync(ctrl, token, port);

            // Сохраняем задачу для последующего отслеживания
            activeTasks[taskKey] = task;
        }

        private void CreateEmulator(Controller ctrl,
            ConcurrentDictionary<string, Task> activeTasks,
            CancellationToken token)
        {
            var taskKey = $"{ctrl.Id}";

            var task = RunControllerLoopAsync(ctrl, token);

            // Сохраняем задачу для последующего отслеживания
            activeTasks[taskKey] = task;
        }

        #endregion

        #region Port

        private async Task<ISharedSerialPortService> OpenPort(Controller controller, CancellationToken ct)
        {
            switch (controller.Type)
            {
                case ControllerType.None:
                    break;
                case ControllerType.Lanfeng:
                    return await LanfengPortOpen(controller, ct);
                case ControllerType.Gilbarco:
                    return await GilbarcoPortOpen(controller, ct);
                case ControllerType.Emulator:
                    break;
                case ControllerType.PKElectronics:
                    break;
                case ControllerType.TechnoProjekt:
                    break;
                default:
                    break;
            }

            return null;
        }

        private async Task<ISharedSerialPortService> GilbarcoPortOpen(Controller controller, CancellationToken ct)
        {
            if (controller.Settings is GilbarcoControllerSettings settings)
            {
                var key = new PortKey(
                    portName: controller.ComPort,
                    baudRate: controller.BaudRate, // TWOTP: фиксированный битрейт 5787 ±0.5%
                    parity: settings.Parity, // TWOTP: Even parity
                    dataBits: 8,
                    stopBits: StopBits.One
                );
                var options = new SerialPortOptions(
                    RtsEnable: false,
                    DtrEnable: false,
                    ReadTimeoutMs: 300,
                    WriteTimeoutMs: 300,
                    ReadBufferSize: 1024,
                    WriteBufferSize: 1024
                );
                try
                {
                    _lease = await _portManager.AcquireAsync(key, options, ct);
                    return _lease.Port;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось запустить TWOTP polling");
                }
            }
            return null;
        }

        private async Task<ISharedSerialPortService> LanfengPortOpen(Controller controller, CancellationToken ct)
        {
            if (controller.Settings is LanfengControllerSettings settings)
            {
                var key = new PortKey(
                    portName: controller.ComPort,
                    baudRate: controller.BaudRate, // TWOTP: фиксированный битрейт 5787 ±0.5%
                    parity: Parity.None, // TWOTP: Even parity
                    dataBits: 8,
                    stopBits: StopBits.One
                );
                var options = new SerialPortOptions(
                    RtsEnable: false,
                    DtrEnable: false,
                    ReadTimeoutMs: 3000,
                    WriteTimeoutMs: 3000,
                    ReadBufferSize: 1024,
                    WriteBufferSize: 1024
                );
                try
                {
                    _lease = await _portManager.AcquireAsync(key, options, ct);
                    return _lease.Port;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не удалось запустить TWOTP polling");
                }
            }
            return null;
        }

        #endregion
    }
}
