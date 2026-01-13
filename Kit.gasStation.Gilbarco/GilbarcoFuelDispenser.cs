using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Gilbarco
{
    /// <summary>
    /// Сервис для работы с колонкой Gilbarco через двухпроводной протокол (TWOTP).
    /// Поддерживает статус-опрос, авторизацию, пресеты, запрос транзакции и т.д.
    /// </summary>
    public sealed class GilbarcoFuelDispenser : FuelDispenserServiceBase
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
        private const int _defaultTimeoutMs = 3000;
        private ILogger _logger;

        #endregion

        #region Constructors

        public GilbarcoFuelDispenser(
            Controller controller,
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

        #region Protected Overrides

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                var key = new PortKey(
                    portName: Controller.ComPort,
                    baudRate: 5787, // TWOTP: фиксированный битрейт 5787 ±0.5%
                    parity: Parity.Even, // TWOTP: Even parity
                    dataBits: 8,
                    stopBits: StopBits.One
                );

                _hub = _hubClient.Connection;

                _hub.On<StartPollingCommand>("StartPolling", async _ => await StartPollingAsync(key, token));
                _hub.On<StopPollingCommand>("StopPolling", async _ => await StopPollingAsync(key));

                _hub.On<string, decimal>("SetPriceAsync", async (groupName, price) =>
                    await SetPriceAsync(groupName, price));

                _hub.On<string, decimal, bool>("StartRefuelingAsync", async (groupName, sum, bySum) =>
                    await StartRefuelingAsync(groupName, sum, bySum));

                _hub.On<string>("CompleteRefuelingAsync", async _ =>
                    await CompleteRefuelingAsync());

                _hub.On<string>("GetCountersAsync", async _ =>
                    await ExecuteCommandAsync(Command.RequestPumpTotals, Address, 0));

                await _hubClient.EnsureStartedAsync(token);

                foreach (var column in Controller.Columns)
                {
                    string groupName = $"{Controller.Name}/{column.Name}";
                    await _hub.InvokeAsync("JoinController", groupName);
                    column.GroupName = groupName;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ошибка при открытии подключения к Gilbarco");
            }
        }

        protected override async Task OnTickAsync(CancellationToken token)
        {
            _logger.Information("Gilbarco TWOTP polling запущен на порту {Port}", Controller.ComPort);

            // Инициализация: запрос версии через Special Function 001
            await _pauseGate.WaitAsync(token);
            await ExecuteCommandAsync(Command.SpecialFunction, Address, 0x001); // Version Request

            while (!token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    await ExecuteCommandAsync(Command.Status, Address, 0, ct: token);
                    await Task.Delay(500, token); // TWOTP: опрос раз в ~500 мс
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Ошибка в цикле опроса TWOTP");
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

        #region Command Execution

        private async Task ExecuteCommandAsync(
            Command cmd,
            int pumpId,
            int subCommandOrData,
            decimal? value = null,
            int expectedLength = 0,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("COM-порт не инициализирован");

            await _pauseGate.WaitAsync(ct);
            await _exclusive.WaitAsync(ct);

            try
            {
                var frame = _protocolParser.BuildRequest(cmd, pumpId, subCommandOrData, value);
                _logger.Information("[Tx] {Frame}", BitConverter.ToString(frame));

                var rx = await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: expectedLength > 0 ? expectedLength : 2, // минимум 1 слово (2 байта)
                    writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
                    readTimeoutMs: _defaultTimeoutMs,
                    maxRetries: 3,
                    ct: ct);

                _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

                var parsed = _protocolParser.ParseResponse(rx);
                if (parsed != null)
                {
                    parsed.Group = Controller.Columns.FirstOrDefault()?.GroupName;
                    await _hub.InvokeAsync("PublishStatus", parsed, parsed.Group);

                    // Обновление статуса
                    Status = parsed.Status;
                }
            }
            finally
            {
                _exclusive.Release();
            }
        }

        #endregion

        #region Business Logic

        private async Task SetPriceAsync(string groupName, decimal price)
        {
            var column = Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
            if (column == null) return;

            // TWOTP: команда '2' → Data Block → Price Change
            // price = XXXX (BCD, LSD first), например: 1234 → 0x34, 0x12
            // но в десятичном виде: 12.34 → умножаем на 100 → 1234
            var priceScaled = (int)(price * 100);
            await ExecuteCommandAsync(Command.SendDataToPump, Address, 0x04, priceScaled); // Level 1
        }

        private async Task StartRefuelingAsync(string groupName, decimal amount, bool bySum)
        {
            var column = Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
            if (column == null) return;

            // TWOTP: команда '2' → Preset Data → Volume или Money
            int presetType = bySum ? 2 : 1; // 2 = money, 1 = volume
            var scaled = bySum ? (int)(amount * 100) : (int)(amount * 100); // volume: в сотых, money: в центах

            await ExecuteCommandAsync(Command.SendDataToPump, Address, presetType, scaled);
            await Task.Delay(100, CancellationToken.None);
            await ExecuteCommandAsync(Command.Authorize, Address, 0); // команда '1'
        }

        private async Task CompleteRefuelingAsync()
        {
            // TWOTP: Pump Stop команда '3'
            await ExecuteCommandAsync(Command.PumpStop, Address, 0);
        }

        #endregion

        #region Polling Control

        private async Task StartPollingAsync(PortKey key, CancellationToken token)
        {
            var options = new SerialPortOptions(
                BaudRate: 5787,
                Parity: Parity.Even,
                DataBits: 8,
                StopBits: StopBits.One,
                RtsEnable: false,
                DtrEnable: false,
                ReadTimeoutMs: _defaultTimeoutMs,
                WriteTimeoutMs: 1000,
                ReadBufferSize: 1024,
                WriteBufferSize: 1024
            );

            lock (_pollLock)
            {
                if (_pollingEnabled) return;
            }

            try
            {
                _lease = await _portManager.AcquireAsync(key, options, token);
                _sharedSerialPortService = _lease.Port;

                lock (_pollLock)
                {
                    _pollingEnabled = true;
                }

                _pollingTask = OnTickAsync(token);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось запустить TWOTP polling");
                lock (_pollLock) _pollingEnabled = false;
                if (_lease != null)
                {
                    await _lease.DisposeAsync();
                    _lease = null;
                }
                _sharedSerialPortService = null!;
            }
        }

        private async Task StopPollingAsync(PortKey key)
        {
            Task? toAwait = null;
            lock (_pollLock)
            {
                if (!_pollingEnabled) return;
                _pollingEnabled = false;
                toAwait = _pollingTask;
            }

            if (toAwait != null)
                await toAwait;

            if (_lease != null)
            {
                await _lease.DisposeAsync();
                _lease = null;
            }
            _sharedSerialPortService = null!;

            try
            {
                await _portManager.CloseIfIdleAsync(key);
            }
            catch { }
        }

        #endregion

        #region Logging

        private void CreateLogger()
        {
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_Gilbarco_{safeController}_{safePort}_{controllerId}_{Address}.log";
            string path = Path.Combine(logRoot, fileName);

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
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _logger.Information("Инициализация Gilbarco TWOTP для {Controller}/{Address}", Controller.Name, Address);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_");
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}
