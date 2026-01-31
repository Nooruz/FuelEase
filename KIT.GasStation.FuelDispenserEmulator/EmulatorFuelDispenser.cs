using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KIT.GasStation.FuelDispenserEmulator
{
    public sealed class EmulatorFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private HubConnection _hub;
        private Task _pollingTask;
        private readonly object _pollLock = new();
        private volatile bool _pollingEnabled;
        private bool _hubHandlersRegistered;
        private volatile bool _hardwareAvailable = true;
        private string? _lastAvailabilityReason;
        private int _hubRestartLoop;
        private readonly ControllerResponse _controllerResponse = new() { Status = NozzleStatus.Ready };
        private CancellationToken _token; 

        #endregion

        #region Constructors

        public EmulatorFuelDispenser(Controller controller,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient) : base(controller, sharedSerialPortService, hubClient)
        {
            _hubClient = hubClient;
            _sharedSerialPortService = sharedSerialPortService;

            CreateLogger();
        }

        #endregion

        #region Override Voids

        protected override async Task OnTickAsync()
        {
            _logger.Information("ТРК Эмулятор запущена, используется порт {Port}", Controller.ComPort);

            var column = Columns.First();

            while (!_token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    // Ждем, если опрос приостановлен
                    _pollingResumedEvent.Wait(_token);

                    await _hub.InvokeAsync("PublishStatus", _controllerResponse, column.GroupName, cancellationToken: _token);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _token)
                {
                    _logger.Information("Опрос ТРК Эмулятор отменён: {Message}", ex.Message);
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message, e.StackTrace);
                    await Task.Delay(1000, _token);
                }
                if (!_pollingEnabled) break;
            }
        }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _token = token;

                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

                _hub = _hubClient.Connection;
                RegisterHubConnectionHandlers();

                _logger.Debug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

                _hub.On<StartPollingCommand>("StartPolling", async e =>
                {
                    await StartPollingAsync(token);
                });

                _hub.On<string>("InitializeConfigurationAsync", async (groupName) =>
                {
                    await InitializeConfigurationAsync(groupName);
                });

                _hub.On<StopPollingCommand>("StopPolling", async e =>
                {
                    //await StopPollingAsync(_portKey);
                });

                _hub.On<string, decimal>("SetPriceAsync", async (groupName, price) =>
                {
                    SetPriceAsync(groupName, price);
                });

                _hub.On<string, decimal, bool>("StartFuelingAsync", async (groupName, sum, bySum) =>
                {
                    await StartFuelingAsync(groupName, sum, bySum);
                });

                _hub.On<string>("CompleteFuelingAsync", async (groupName) =>
                {
                    await CompleteFuelingAsync(groupName);
                });

                _hub.On<string, bool>("ChangeControlModeAsync", async (groupName, isProgramMode) =>
                {
                    //await ChangeControlModeAsync(groupName, isProgramMode);
                });

                _hub.On<string>("StopFuelingAsync", async (groupName) =>
                {
                    //await StopFuelingAsync(groupName);
                });

                _hub.On<string>("ResumeFuelingAsync", async (groupName) =>
                {
                    //await ResumeFuelingAsync(groupName);
                });

                _hub.On<string>("GetStatusByAddressAsync", async (groupName) =>
                {
                    //await GetStatusByAddressAsync(groupName);
                });

                _hub.On<string>("PausePollingAsync", async (groupName) =>
                {
                    if (Columns.Any(c => c.GroupName == groupName))
                    {
                        await PausePollingAsync();
                    }
                });

                _hub.On<string>("ResumePollingAsync", async (groupName) =>
                {
                    if (Columns.Any(c => c.GroupName == groupName))
                    {
                        await ResumePollingAsync();
                    }
                });

                _hub.On<string>("GetCountersAsync", async (groupName) =>
                {
                    var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                    if (column is not null)
                    {
                        //await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
                    }
                });

                // 1. СНАЧАЛА запускаем SignalR (синхронно)
                await _hubClient.EnsureStartedAsync(token);

                // 3. Присоединяемся к группам
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);

            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка в OnOpenAsync: {Message}", e.Message);
                throw;
            }
        }

        #endregion

        #region Private Voids

        private async Task StartPollingAsync(CancellationToken token)
        {
            if (_pollingTask == null || _pollingTask.IsCompleted)
            {
                lock (_pollLock)
                {
                    if (_pollingEnabled) return;      // уже запущено
                }

                try
                {
                    // 2) Только после успешного Acquire включаем флаг
                    lock (_pollLock)
                    {
                        _pollingEnabled = true;
                    }

                    // 3) Стартуем цикл опроса (lease остаётся жить в поле)
                    _pollingTask = OnTickAsync();
                    await BroadcastWorkerAvailabilityAsync(true, "Polling started");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось стартовать polling");
                    lock (_pollLock) { _pollingEnabled = false; }
                    await BroadcastWorkerAvailabilityAsync(false);
                }
            }

            var column = Columns.First();

            _controllerResponse.Address = column.Address;
            _controllerResponse.StatusAddress = column.Nozzle;
            _controllerResponse.Group = column.GroupName;

            await _hub.InvokeAsync("PublishStatus", _controllerResponse, column.GroupName, cancellationToken: token);
        }

        private async Task InitializeConfigurationAsync(string groupName)
        {
            
        }

        private async Task StartFuelingAsync(string groupName, decimal value, bool bySum)
        {
            try
            {
                var column = GetColumnByGroupName(groupName);

                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                decimal sum = bySum ? value : Math.Round(value * column.Price, 2);
                decimal quantity = bySum ? Math.Round(value / column.Price, 3) : value;

                var rnd = new Random();
                decimal receivedQuantity = 0;


                while (receivedQuantity < quantity)
                {
                    decimal step = (decimal)(rnd.NextDouble() * 0.5); // 0..10 за тик
                    receivedQuantity += step;

                    if (receivedQuantity > quantity)
                    {
                        receivedQuantity = quantity;
                    }

                    _controllerResponse.Status = NozzleStatus.PumpWorking;

                    _controllerResponse.Group = groupName;

                    _controllerResponse.Quantity = receivedQuantity;
                    _controllerResponse.Sum = Math.Round(receivedQuantity * column.Price, 2);

                    await _hub.InvokeAsync("PublishStatus", _controllerResponse, groupName);

                    await Task.Delay(300);
                }

                await Task.Delay(1000);

                _controllerResponse.Status = NozzleStatus.PumpStop;
                _controllerResponse.Address = column.Address;
                _controllerResponse.StatusAddress = column.Address;
                await _hub.InvokeAsync("PublishStatus", _controllerResponse, groupName);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        private void SetPriceAsync(string groupName, decimal price)
        {
            try
            {
                var column = GetColumnByGroupName(groupName);

                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                column.Price = price;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        private async Task CompleteFuelingAsync(string groupName)
        {
            _controllerResponse.Status = NozzleStatus.Ready;
            await _hub.InvokeAsync("PublishStatus", _controllerResponse, groupName);
        }

        #endregion

        #region Helpers

        private Column? GetColumnByGroupName(string groupName)
        {
            return Columns.FirstOrDefault(c => c.GroupName == groupName);
        }

        #endregion

        #region Пауза и продолжения опроса

        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
        private readonly SemaphoreSlim _commandGate = new(1, 1);

        private async Task PausePollingAsync()
        {
            _pollingResumedEvent.Reset();

            // ⛔ ждём, пока текущая команда ДОРАБОТАЕТ
            await _commandGate.WaitAsync();
            _commandGate.Release();
        }

        private async Task ResumePollingAsync()
        {
            _pollingResumedEvent.Set();
            await Task.CompletedTask;
        }

        private async Task ExecuteCommandSafeAsync(
            Func<Task> command,
            CancellationToken token)
        {
            await _commandGate.WaitAsync(token);
            try
            {
                await command(); // <-- вот тут команда гарантированно ДОРАБАТЫВАЕТ
            }
            finally
            {
                _commandGate.Release();
            }
        }

        #endregion

        #region Hub

        private void RegisterHubConnectionHandlers()
        {
            if (_hubHandlersRegistered || _hub is null)
                return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;
        }

        private Task OnHubReconnecting(Exception? error)
        {
            _logger.Warning("Потеряно соединение с сервером: {Message}", error?.Message ?? "unknown");
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            _logger.Information("Переподключен с сервером. ConnectionId={ConnectionId}", connectionId);
            try
            {
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось повторно присоединиться к группам после переподключения");
            }
        }

        private Task OnHubClosed(Exception? error)
        {
            _logger.Error(error, "Соединение с SignalR было закрыто");
            return RestartHubConnectionLoopAsync();
        }

        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null)
                return Task.CompletedTask;

            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hub.StartAsync();
                            await JoinWorkerGroupsAsync();
                            await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 секунд");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubRestartLoop, 0);
                }
            });
        }

        private async Task JoinWorkerGroupsAsync()
        {
            if (_hub is null || Controller?.Columns is null)
                return;

            // Ждем, пока соединение станет активным
            var timeout = TimeSpan.FromSeconds(10);
            var stopwatch = Stopwatch.StartNew();

            while (_hub.State != HubConnectionState.Connected && stopwatch.Elapsed < timeout)
            {
                await Task.Delay(100);
            }

            if (_hub.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Не удалось подключиться к SignalR за отведенное время");

            foreach (var item in Controller.Columns)
            {
                if (string.IsNullOrWhiteSpace(item.GroupName))
                {
                    item.GroupName = $"{Controller.Name}/{item.Name}";
                }

                await _hub.InvokeAsync("JoinController", item.GroupName, true);
            }
        }

        private async Task BroadcastWorkerAvailabilityAsync(bool isAvailable, string? reason = null, bool force = false)
        {
            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            if (!force &&
                _hardwareAvailable == isAvailable &&
                string.Equals(_lastAvailabilityReason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            _hardwareAvailable = isAvailable;
            _lastAvailabilityReason = sanitizedReason;

            if (_hub is null || _hub.State != HubConnectionState.Connected)
                return;

            if (Controller?.Columns is null)
                return;

            var groups = Controller.Columns
                .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
                .Select(c => c.GroupName!);

            var tasks = groups.Select(group => SendAvailabilityAsync(group, isAvailable, sanitizedReason));
            await Task.WhenAll(tasks);
        }

        private async Task SendAvailabilityAsync(string groupName, bool isAvailable, string? reason)
        {
            try
            {
                var report = new WorkerAvailabilityReport
                {
                    GroupName = groupName,
                    IsAvailable = isAvailable,
                    Reason = reason
                };
                await _hub.InvokeAsync("ReportWorkerAvailability", report);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось отправить состояние worker для {Group}", groupName);
            }
        }

        private static bool IsCriticalSerialException(Exception ex) =>
            ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException;

        #endregion

        #region Logs

        private void CreateLogger()
        {
            // создаём общую папку для логов ТРК
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            // безопасное имя файла (уникальное для экземпляра)
            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_{Controller.Type}_{safeController}_{safePort}_{controllerId}_{Address}.log";
            string path = Path.Combine(logRoot, fileName);

            // отдельный Serilog для файла инстанса
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Controller", Controller.Name)
                .Enrich.WithProperty("Address", Address)
                .Enrich.WithProperty("ComPort", Controller.ComPort)
                .WriteTo.File(
                    path: path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _logger.Information("Инициализация Emulator для {Controller}/{Address} на порту {Port}",
            Controller.Name, Address, Controller.ComPort);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_"); // заменяем недопустимые символы
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}
