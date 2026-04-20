using KIT.App.Infrastructure.Factories;
using KIT.App.Infrastructure.Services.Hubs;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Licensing.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Collections.Concurrent;
using System.IO.Ports;

namespace KIT.GasStation.Web
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IFuelDispenserFactory _fuelDispenserFactory;
        private readonly IFuelDispenserRegistry _registry;
        private readonly IHubCommandRouter _router;
        private readonly IPortManager _portManager;
        private readonly IHubClient _hubClient;
        private readonly LicenseGuardService _licenseGuard;
        private PortLease? _lease;

        public Worker(ILogger<Worker> logger,
            IHardwareConfigurationService hardwareConfigurationService,
            IFuelDispenserFactory fuelDispenserFactory,
            IPortManager portManager,
            IFuelDispenserRegistry registry,
            IHubCommandRouter router,
            IHubClient hubClient,
            LicenseGuardService licenseGuard)
        {
            _logger = logger;
            _hardwareConfigurationService = hardwareConfigurationService;
            _fuelDispenserFactory = fuelDispenserFactory;
            _portManager = portManager;
            _registry = registry;
            _router = router;
            _hubClient = hubClient;
            _licenseGuard = licenseGuard;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Worker started");

            // Ждём завершения начальной проверки лицензии (активация + валидация).
            await _licenseGuard.InitialCheckCompleted.WaitAsync(stoppingToken);

            if (!_licenseGuard.IsLicenseValid)
            {
                Log.Error("Worker: лицензия не прошла проверку, запуск прерван");
                return;
            }

            Log.Information("Worker: лицензия подтверждена (статус: {Status}), запускаю сервисы",
                _licenseGuard.CurrentStatus);

            await _hubClient.EnsureStartedAsync(stoppingToken);
            _router.RegisterHandlers();

            var controllers = await _hardwareConfigurationService.GetControllersAsync();

            var activeTasks = new ConcurrentDictionary<string, Task>();
            var activeDispensers = new List<IFuelDispenserService>();

            try
            {
                foreach (var ctrl in controllers)
                {
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

                        var dispenser = _fuelDispenserFactory.Create(controller.Type);
                        dispenser.Controller = controller;

                        _registry.Register(dispenser);
                        activeDispensers.Add(dispenser);

                        activeTasks[taskKey] = RunControllerLoopAsync(dispenser, stoppingToken);

                        foreach (var column in controller.Columns)
                        {
                            await _hubClient.Connection.InvokeAsync(
                                "JoinController", column.GroupName, true, stoppingToken);
                        }
                    }
                }

                // Ждём завершения всех задач (либо отмены stoppingToken)
                await Task.WhenAll(activeTasks.Values);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker: остановка по запросу");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в Worker.ExecuteAsync");
            }
            finally
            {
                // Снимаем все диспенсеры с регистрации при завершении
                foreach (var dispenser in activeDispensers)
                {
                    _registry.Remove(dispenser);
                }
            }
        }

        private async Task RunControllerLoopAsync(IFuelDispenserService dispenser,
            CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Запуск/перезапуск цикла для контроллера {Id}", dispenser.Controller.Id);

                    await dispenser.RunAsync(token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Остановка цикла для контроллера {Id}", dispenser.Controller.Id);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в цикле для контроллера {Id}", dispenser.Controller.Id);
                    // Ждём перед повторной попыткой
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                finally
                {
                    if (dispenser != null)
                        await dispenser.DisposeAsync();
                }
            }
        }

        #region Helpers

        private void CreateEmulator(Controller ctrl,
            ConcurrentDictionary<string, Task> activeTasks,
            CancellationToken token)
        {
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
                    Columns = new([.. ctrl.Columns.Where(c => c.Address == addr)])
                };

                var taskKey = $"{ctrl.Id}_{addr}";
            }
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
                    baudRate: controller.BaudRate,
                    parity: settings.Parity,
                    dataBits: 8,
                    stopBits: StopBits.One
                );
                var options = new SerialPortOptions(
                    RtsEnable: false,
                    DtrEnable: false,
                    ReadTimeoutMs: 100,
                    WriteTimeoutMs: 100,
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
                    _logger.LogError(ex, "Не удалось открыть порт Gilbarco");
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
                    baudRate: controller.BaudRate,
                    parity: Parity.None,
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
                    _logger.LogError(ex, "Не удалось открыть порт Lanfeng");
                }
            }
            return null;
        }

        #endregion
    }
}
