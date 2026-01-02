using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Lanfeng
{
    /// <summary>
    /// Сервис для работы с колонкой Lanfeng через COM-порт.
    /// Прослушивает статусы и обрабатывает команды.
    /// </summary>
    public sealed class LanfengFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly IProtocolParser _protocolParser;
        private readonly IPortManager _portManager;
        private readonly IHubClient _hubClient;
        private readonly AsyncManualResetEvent _pauseGate = new(initialState: true);
        private readonly SemaphoreSlim _exclusive = new(1, 1);
        private ISharedSerialPortService _sharedSerialPortService;
        private HubConnection _hub;
        private volatile bool _pollingEnabled;
        private bool _stopTickStatus = false;
        private Task _pollingTask;
        private PortLease? _lease;
        private readonly object _pollLock = new();
        private const int _frameLen = 14;
        private ILogger _logger;
        private LanfengControllerType _controllerType;
        private volatile bool _hardwareAvailable = true;
        private string? _lastAvailabilityReason;
        private bool _hubHandlersRegistered;
        private int _hubRestartLoop;    

        #endregion

        #region Constructors

        public LanfengFuelDispenser(Controller controller,
            int address,
            IProtocolParserFactory protocolParserFactory,
            IPortManager portManager,
            IHubClient hubClient) 
            : base(controller, address, protocolParserFactory, portManager, hubClient)
        {
            _protocolParser = protocolParserFactory.CreateIProtocolParser(Controller.Type);
            _portManager = portManager;
            _hubClient = hubClient;

            if (Columns != null)
            {
                _controllerType = Columns.Count > 1 ? LanfengControllerType.Multi : LanfengControllerType.Single;
            }

            CreateLogger();
        }

        #endregion

        #region Protected Voids

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                var key = new PortKey(
                    portName: Controller.ComPort,                // например, "COM3"
                    baudRate: Controller.BaudRate,               // напр., 9600
                    parity: Parity.None,                // System.IO.Ports.Parity
                    dataBits: 8,              // обычно 8
                    stopBits: StopBits.One               // StopBits.One и т.п.
                );

                _hub = _hubClient.Connection;
                RegisterHubConnectionHandlers();

                _hub.On<StartPollingCommand>("StartPolling", async e =>
                {
                    await StartPollingAsync(key, token);
                });

                _hub.On<StopPollingCommand>("StopPolling", async e =>
                {
                    await StopPollingAsync(key);
                });

                _hub.On<string, decimal>("SetPriceAsync", async (groupName, price) =>
                {
                    await SetPriceAsync(groupName, price);
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
                    await ChangeControlModeAsync(groupName, isProgramMode);
                });

                _hub.On<string>("StopFuelingAsync", async (groupName) =>
                {
                    await StopFuelingAsync(groupName);
                });

                _hub.On<string>("ResumeFuelingAsync", async (groupName) =>
                {
                    await ResumeFuelingAsync(groupName);
                });

                _hub.On<string>("GetCountersAsync", async (groupName) =>
                {
                    _stopTickStatus = true;
                    var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                    if (column is not null)
                    {
                        await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
                    }
                    _stopTickStatus = false;
                });

                await _hubClient.EnsureStartedAsync(token);

                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        /// <summary>
        /// Цикл опроса статуса ТРК.
        /// Выполняется пока не придёт сигнал отмены.
        /// </summary>
        protected override async Task OnTickAsync(CancellationToken token)
        {
            _logger.Information("ТРК Lanfeng запущена, используется порт {Port}", Controller.ComPort);

            if (Status is NozzleStatus.Unknown)
            {
                await ExecuteCommandAsync(Command.Status, Address, 0);

                // Получаем версию прошивки
                await _pauseGate.WaitAsync(token);
                await ExecuteCommandAsync(Command.FirmwareVersion, Address, 0);

                // Инициализация по пистолетам
                await _pauseGate.WaitAsync(token);
            }

            while (!token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    // опрос — одна команда через универсальный метод
                    if (Status is NozzleStatus.Ready or 
                        NozzleStatus.PumpWorking or
                        NozzleStatus.WaitingRemoved && !_stopTickStatus)
                    {
                        await ExecuteCommandAsync(Command.Status, Address, 0, null, ct: token);
                    }
                }
                catch (OperationCanceledException) { /* штатно */ }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message, e.StackTrace);
                }
                if (!_pollingEnabled) break;
            }
        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            (_logger as IDisposable)?.Dispose();
            await base.DisposeAsync();
        }

        #endregion

        #region Private Voids

        private async Task ExecuteCommandAsync(
            Command cmd,
            int controllerAddress,
            int nozzleMask,
            decimal? value = null,
            int expectedLength = _frameLen,
            int writeToReadDelayMs = 0,
            int readTimeoutMs = 3000,
            int maxRetries = 2,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("Последовательный порт еще не получен.");

            _logger.Information(
                "ExecuteCommandAsync start: {Command} addr={ControllerAddress} nozzle={NozzleMask} expected={ExpectedLength} timeoutMs={ReadTimeoutMs}",
                cmd, controllerAddress, nozzleMask, expectedLength, readTimeoutMs);

            await _pauseGate.WaitAsync(ct);     // уважаем паузу
            await _exclusive.WaitAsync(ct);     // логически сериализуем окно команд
            try
            {
                var commandStart = Stopwatch.StartNew();
                var sd = _controllerType;
                var frame = _protocolParser.BuildRequest(cmd, controllerAddress, nozzleMask, value, controllerType: _controllerType);
                _logger.Information("[Tx] {Tx}", BitConverter.ToString(frame));

                byte[] rx;
                try
                {
                    _logger.Information(
                        "Serial write/read begin: {Command} port={Port} retries={MaxRetries}",
                        cmd, _sharedSerialPortService.PortName, maxRetries);
                    rx = await _sharedSerialPortService.WriteReadAsync(
                        frame,
                        expectedRxLength: expectedLength,
                        writeToReadDelayMs: writeToReadDelayMs,
                        readTimeoutMs: readTimeoutMs,
                        maxRetries: maxRetries,
                        ct: ct);
                    await BroadcastWorkerAvailabilityAsync(true);
                }
                catch (Exception ex) when (IsCriticalSerialException(ex))
                {
                    _logger.Error(ex, "Ошибка обмена с COM-портом, колонка будет отмечена как недоступная");
                    await BroadcastWorkerAvailabilityAsync(false, ex.Message);
                    throw;
                }

                var resp = _protocolParser.ParseResponse(rx);

                if (resp is not null)
                {
                    Column? column = null;

                    if (resp.Command is Command.CounterLiter)
                    {
                        if (_controllerType == LanfengControllerType.Single)
                        {
                            column = Columns.FirstOrDefault();
                        }
                        else
                        {
                            column = Columns.FirstOrDefault(c => c.LanfengAddress == resp.Address);
                        }
                    }
                    else
                    {
                        if (_controllerType == LanfengControllerType.Single)
                        {
                            column = Columns.FirstOrDefault();
                        }
                        else
                        {
                            column = Columns.FirstOrDefault(c => c.LanfengAddress == resp.StatusAddress);
                        }
                    }

                    if (column is not null)
                    {
                        resp.Group = column.GroupName;
                        await _hub.InvokeAsync("PublishStatus", resp, column.GroupName);
                    }

                    await HandleColumnLiftedAsync(resp);

                    Status = resp.Status;
                }

                _logger.Information("[Rx] {Rx}", BitConverter.ToString(rx));
                _logger.Information(
                    "ExecuteCommandAsync completed: {Command} in {ElapsedMs}ms",
                    cmd, commandStart.ElapsedMilliseconds);
            }
            finally
            {
                _exclusive.Release();
            }
        }

        private async Task ExecuteSequenceAsync(
            Func<Func<Command, int, int, decimal?, Task<byte[]>>, Task> body,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("Последовательный порт еще не получен.");

            await _pauseGate.WaitAsync(ct);
            await _exclusive.WaitAsync(ct);
            try
            {
                // локальная отправка без повторного захвата _exclusive
                async Task<byte[]> send(Command c, int addr, int mask, decimal? val)
                {
                    var frame = _protocolParser.BuildRequest(c, addr, mask, val);
                    _logger.Information("[Tx] {Tx}", BitConverter.ToString(frame));

                    var rx = await _sharedSerialPortService.WriteReadAsync(
                        frame, expectedRxLength: _frameLen, writeToReadDelayMs: 0, readTimeoutMs: 3000, maxRetries: 2, ct);
                    _logger.Information("[Rx] {Rx}", BitConverter.ToString(rx));
                    return rx;
                }

                await body(send); // твоя последовательность (например: A4 -> A2)
            }
            finally
            {
                _exclusive.Release();
            }
        }

        private async Task ChangeControlModeAsync(string groupName, bool isProgramMode)
        {
            try
            {
                _stopTickStatus = true;

                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                var cmd = isProgramMode ? Command.ProgramControlMode : Command.KeyboardControlMode;
                await ExecuteCommandAsync(cmd, Address, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false;
            }
        }

        private async Task SetPriceAsync(string groupName, decimal price)
        {
            try
            {
                _stopTickStatus = true; // временно приостанавливаем опрос

                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                await ExecuteCommandAsync(Command.ChangePrice, Address, column.LanfengAddress, price);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false; // возобновляем опрос
            }
        }

        private async Task StartFuelingAsync(string groupName, decimal sum, bool bySum)
        {
            try
            {
                _stopTickStatus = true;

                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                var cmd = bySum ? Command.StartFuelingSum : Command.StartFuelingQuantity;
                await ExecuteCommandAsync(cmd, Address, column.LanfengAddress, sum);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false;
            }
        }

        private async Task StopFuelingAsync(string groupName)
        {
            try
            {
                _stopTickStatus = true;

                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                await ExecuteCommandAsync(Command.StopFueling, Address, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false;
            }
        }

        private async Task ResumeFuelingAsync(string groupName)
        {
            try
            {
                _stopTickStatus = true;
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                await ExecuteCommandAsync(Command.ContinueFueling, Address, column.LanfengAddress);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false;
            }
        }

        /// <summary>
        /// Метод остановки цикла опроса.
        /// </summary>
        private async Task StopPollingAsync(PortKey key)
        {
            Task? toAwait = null;
            lock (_pollLock)
            {
                if (!_pollingEnabled) return;     // уже остановлено
                _pollingEnabled = false;
                toAwait = _pollingTask;
            }

            // даём циклу корреткно выйти
            if (toAwait is not null)
            {
                try { await toAwait; } catch { /* игнор логика выхода */ }
            }

            // освобождаем lease и физически закрываем COM, если больше никто не держит
            if (_lease is not null)
            {
                await _lease.DisposeAsync();
                _lease = null;
            }
            _sharedSerialPortService = null!;

            // По желанию: мягкая уборка простаивающего порта
            try { await _portManager.CloseIfIdleAsync(key); } catch { }
        }

        /// <summary>
        /// Метод запуска цикла опроса.
        /// </summary>
        private async Task StartPollingAsync(PortKey key, CancellationToken token)
        {
            var options = new SerialPortOptions(
                BaudRate: Controller.BaudRate,
                Parity: Parity.None,
                DataBits: 8,
                StopBits: StopBits.One,
                RtsEnable: false,            // рукопожатие отсутствует (no handshaking)
                DtrEnable: false,
                ReadTimeoutMs: 3000,
                WriteTimeoutMs: 1000,
                ReadBufferSize: 64 * 1024,
                WriteBufferSize: 64 * 1024
            );

            if (_pollingTask == null || _pollingTask.IsCompleted)
            {
                lock (_pollLock)
                {
                    if (_pollingEnabled) return;      // уже запущено
                }

                try
                {
                    // 1) Берём ЛИЗУ В ПОЛЕ
                    _lease = await _portManager.AcquireAsync(key, options, token);
                    _sharedSerialPortService = _lease.Port;

                    // 2) Только после успешного Acquire включаем флаг
                    lock (_pollLock)
                    {
                        _pollingEnabled = true;
                    }

                    // 3) Стартуем цикл опроса (lease остаётся жить в поле)
                    _pollingTask = OnTickAsync(token);
                    await BroadcastWorkerAvailabilityAsync(true, "Polling started");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось стартовать polling");
                    lock (_pollLock) { _pollingEnabled = false; }

                    // если успели получить лизу — аккуратно отпустим и обнулим
                    if (_lease is not null)
                    {
                        await _lease.DisposeAsync();
                        _lease = null;
                    }
                    _sharedSerialPortService = null!;
                    await BroadcastWorkerAvailabilityAsync(false);
                }
            }
        }

        private async Task HandleColumnLiftedAsync(ControllerResponse response)
        {
            if (response.Data is null) return;

            // Протокол: в младшем полубайте (low nibble) порядковый номер пистолета (1..n).
            int liftedOrdinal = response.Data[12] & 0x0F;

            if (liftedOrdinal == 0)
            {
                var liftedColumn = Columns.FirstOrDefault(c => c.IsLifted);
                if (liftedColumn is not null)
                {
                    liftedColumn.IsLifted = false;
                    await _hub.InvokeAsync("ColumnLiftedChanged", liftedColumn.GroupName, false);
                }
            }
            else
            {
                var column = Columns.FirstOrDefault(c => c.LanfengAddress == liftedOrdinal);
                if (column is not null)
                {
                    column.IsLifted = true;
                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, true);
                }
            }
        }

        private async Task CompleteFuelingAsync(string groupName)
        {
            try
            {
                _stopTickStatus = true;

                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                await ExecuteCommandAsync(Command.CompleteFueling, Address, column.Address);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                _stopTickStatus = false;
            }
        }

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
            _logger.Warning("Потеряно соединение с SignalR: {Message}", error?.Message ?? "unknown");
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            _logger.Information("SignalR переподключен. ConnectionId={ConnectionId}", connectionId);
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

            _logger.Information("Инициализация Lanfeng для {Controller}/{Address} на порту {Port}",
            Controller.Name, Address, Controller.ComPort);
        }

        // sanitization для имени файла
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

