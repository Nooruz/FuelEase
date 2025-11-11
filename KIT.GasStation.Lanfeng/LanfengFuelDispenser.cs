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
using System.IO.Ports;
using System.Net;
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
        private Task _pollingTask;
        private PortLease? _lease;
        private readonly object _pollLock = new();
        private const int _frameLen = 14;
        private ILogger _logger;

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

                _hub.On<string, decimal, bool>("StartRefuelingAsync", async (groupName, sum, bySum) =>
                {
                    await StartRefuelingAsync(groupName, sum, bySum);
                });

                _hub.On<string>("CompleteRefuelingAsync", async (groupName) =>
                {
                    await CompleteRefuelingAsync(groupName);
                });

                _hub.On<string>("GetCountersAsync", async (groupName) =>
                {
                    var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                    if (column is not null)
                    {
                        await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
                    }
                });

                await _hubClient.EnsureStartedAsync();

                foreach (var item in Controller.Columns)
                {
                    string groupName = $"{Controller.Name}/{item.Name}";
                    await _hub.InvokeAsync("JoinController", groupName);
                    item.GroupName = groupName;
                }
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
                // Задаем программное управление
                await _pauseGate.WaitAsync(token);
                await ExecuteCommandAsync(Command.ProgramControlMode, Address, 0);

                // Получаем версию прошивки
                await _pauseGate.WaitAsync(token);
                await ExecuteCommandAsync(Command.FirmwareVersion, Address, 0);

                // Инициализация по пистолетам
                await _pauseGate.WaitAsync(token);
                await InitializeByColumnsAsync(token);
            }

            await ExecuteCommandAsync(Command.Status, Address, 0);

            while (!token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    // опрос — одна команда через универсальный метод
                    if (Status is NozzleStatus.Ready or NozzleStatus.PumpWorking)
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

            await _pauseGate.WaitAsync(ct);     // уважаем паузу
            await _exclusive.WaitAsync(ct);     // логически сериализуем окно команд
            try
            {
                var frame = _protocolParser.BuildRequest(cmd, controllerAddress, nozzleMask, value);
                _logger.Information("[Tx] {Tx}", BitConverter.ToString(frame));

                var rx = await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: expectedLength,
                    writeToReadDelayMs: writeToReadDelayMs,
                    readTimeoutMs: readTimeoutMs,
                    maxRetries: maxRetries,
                    ct: ct);

                var resp = _protocolParser.ParseResponse(rx);

                if (resp is not null)
                {
                    Column? column = null;

                    if (resp.Command is Command.CounterLiter)
                    {
                        column = Columns.FirstOrDefault(c => c.LanfengAddress == resp.Address);
                    }
                    else
                    {
                        column = Columns.FirstOrDefault(c => c.LanfengAddress == resp.StatusAddress);
                    }

                    if (column is not null)
                    {
                        resp.Group = column.GroupName;
                        await _hub.InvokeAsync("PublishStatus", resp, column.GroupName);
                    }

                    await HandleColumnLiftedAsync(resp.Data);

                    Status = resp.Status;
                }

                _logger.Information("[Rx] {Rx}", BitConverter.ToString(rx));
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

        private async Task InitializeByColumnsAsync(CancellationToken ct)
        {
            foreach (var column in Columns)
            {
                // Получает счетчик литров.
                await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
                await Task.Delay(300, ct);
            }
        }

        private async Task SetPriceAsync(string groupName, decimal price)
        {
            try
            {
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
        }

        private async Task StartRefuelingAsync(string groupName, decimal sum, bool bySum)
        {
            try
            {
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                var cmd = bySum ? Command.StartFillingSum : Command.StartFillingQuantity;
                await ExecuteCommandAsync(cmd, Address, column.LanfengAddress, sum);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
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
                }
            }
        }

        private async Task HandleColumnLiftedAsync(byte[] response)
        {
            // Протокол: в младшем полубайте (low nibble) порядковый номер пистолета (1..n).
            int liftedOrdinal = response[12] & 0x0F;

            foreach (var column in Columns)
            {
                bool isLifted = column.LanfengAddress == liftedOrdinal;

                if (column.IsLifted != isLifted)
                {
                    column.IsLifted = isLifted;
                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, isLifted);
                }
            }
        }

        private async Task CompleteRefuelingAsync(string groupName)
        {
            try
            {
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                await ExecuteCommandAsync(Command.CompleteFilling, Address, column.Address);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        #endregion

        #region Logs

        private void CreateLogger()
        {
            // создаём каталог
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logDir);

            // безопасное имя файла
            string safeController = Sanitize($"{Controller.Name}");
            string fileName = $"TRK_{Controller.Type}_{safeController}_{Address}.log";
            string path = Path.Combine(logDir, fileName);

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

