using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Реализация общего владельца порта с очередью I/O.
    /// </summary>
    public class SharedSerialPortService : ISharedSerialPortService
    {
        #region Private Members

        private readonly SemaphoreSlim _io = new(1, 1); // общий семафор на I/O (half-duplex)
        private SerialPort? _port;
        private volatile bool _isOpen;
        private PortKey _key;
        private SerialPortOptions? _options;
        private readonly ILogger<SharedSerialPortService> _logger;

        #endregion

        #region Public Properties

        public bool IsOpen => _isOpen;
        public string PortName => _key.PortName;

        #endregion

        #region Constructors

        public SharedSerialPortService(ILogger<SharedSerialPortService>? logger = null)
        {
            _logger = logger ?? NullLogger<SharedSerialPortService>.Instance;
        }

        #endregion

        #region Public Voids

        /// <inheritdoc/>
        public async Task OpenAsync(PortKey key, SerialPortOptions options, CancellationToken ct)
        {
            // Идемпотентное открытие: если уже открыт под тем же ключом — ничего не делаем.
            if (_isOpen && _port is not null && _port.IsOpen && _key.Equals(key))
                return;

            // Закрываем предыдущий инстанс, если был открыт под другим ключом.
            await CloseAsync();

            _key = key;
            _options = options;

            // Открытие делаем синхронным внутри Task.Run, чтобы не блокировать вызывающий поток.
            await OpenWithRetriesAsync(key, options, ct);
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            // Идемпотентно закрываем и освобождаем.
            try
            {
                var p = _port;
                _port = null;
                _isOpen = false;

                if (p is not null)
                {
                    if (p.IsOpen) p.Close();
                    p.Dispose();
                }
            }
            catch
            {
                // Игнорируем ошибки закрытия — обычно безопасно.
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<byte[]> WriteReadAsync(byte[] tx,
            int expectedRxLength,
            int writeToReadDelayMs = 50,
            int readTimeoutMs = 3000,
            int maxRetries = 2,
            CancellationToken ct = default)
        {
            var portLabel = _key.PortName ?? "<unset>";
            _logger.LogDebug("Serial {Port}: waiting for I/O lock.", portLabel);
            await _io.WaitAsync(ct);
            try
            {
                _logger.LogDebug("Serial {Port}: acquired I/O lock.", portLabel);
                if (PortRequiresReopen())
                {
                    _logger.LogWarning("Serial {Port}: port requires reopen before I/O.", portLabel);
                    await RecoverPortAsync(ct);
                }
                // Простой retry-цикл: если таймаут/IO — ещё попытки (maxRetries).
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        _logger.LogDebug("Serial {Port}: write {TxLength} bytes (attempt {Attempt}/{MaxRetries}).",
                            portLabel, tx.Length, attempt, maxRetries);
                        // === WRITE ===
                        await _port.BaseStream.WriteAsync(tx, 0, tx.Length, ct);
                        await _port.BaseStream.FlushAsync(ct);
                        _logger.LogDebug("Serial {Port}: write completed.", portLabel);

                        if (writeToReadDelayMs > 0)
                            await Task.Delay(writeToReadDelayMs, ct);

                        // === READ EXACTLY expectedRxLength ===
                        var buf = new byte[expectedRxLength];
                        var sw = Stopwatch.StartNew();
                        var total = 0;
                        _logger.LogDebug("Serial {Port}: read {ExpectedLength} bytes with timeout {ReadTimeoutMs}ms.",
                            portLabel, expectedRxLength, readTimeoutMs);

                        while (total < expectedRxLength)
                        {
                            ct.ThrowIfCancellationRequested();

                            var leftMs = readTimeoutMs - (int)sw.ElapsedMilliseconds;
                            if (leftMs <= 0)
                                throw new TimeoutException("Истёк таймаут чтения ответа от устройства.");

                            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                            cts.CancelAfter(leftMs);

                            var n = await _port.BaseStream.ReadAsync(buf.AsMemory(total, expectedRxLength - total), cts.Token);
                            if (n > 0)
                            {
                                total += n;
                                _logger.LogDebug("Serial {Port}: read {ReadBytes}/{ExpectedLength} bytes.",
                                    portLabel, total, expectedRxLength);
                                continue;
                            }
                            // n == 0 — просто ждём дальше до дедлайна
                        }

                        // подчистим возможные лишние байты
                        if (_port.BytesToRead > 0) _port.DiscardInBuffer();
                        _logger.LogDebug("Serial {Port}: read completed ({ExpectedLength} bytes).", portLabel, expectedRxLength);

                        return buf;
                    }
                    catch (TimeoutException ex) when (attempt <= maxRetries)
                    {
                        _logger.LogWarning(ex,
                            "Serial {Port}: read timeout after {ReadTimeoutMs}ms (attempt {Attempt}/{MaxRetries}).",
                            portLabel, readTimeoutMs, attempt, maxRetries);
                        // небольшой экспоненциальный бэкофф
                        await Task.Delay(50 * attempt, ct);
                        continue;
                    }
                    catch (IOException ex) when (attempt <= maxRetries)
                    {
                        _logger.LogWarning(ex, "Serial {Port}: IO error, recovering port (attempt {Attempt}/{MaxRetries}).",
                            portLabel, attempt, maxRetries);
                        await RecoverPortAsync(ct);
                        await Task.Delay(50 * attempt, ct);
                        continue;
                    }
                    catch (InvalidOperationException ex) when (attempt <= maxRetries)
                    {
                        _logger.LogWarning(ex, "Serial {Port}: invalid operation, recovering port (attempt {Attempt}/{MaxRetries}).",
                            portLabel, attempt, maxRetries);
                        await RecoverPortAsync(ct);
                        await Task.Delay(50 * attempt, ct);
                        continue;
                    }
                }
            }
            finally
            {
                _io.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _io.Dispose();
        }

        #endregion

        #region Private Helpers

        private async Task OpenWithRetriesAsync(PortKey key, SerialPortOptions options, CancellationToken ct)
        {
            var totalTimeout = TimeSpan.FromMilliseconds(Math.Max(0, options.OpenRetryTimeoutMs));
            var retryDelay = TimeSpan.FromMilliseconds(Math.Max(1, options.OpenRetryDelayMs));
            var sw = Stopwatch.StartNew();
            Exception? lastError = null;

            while (true)
            {
                try
                {
                    // Открытие делаем синхронным внутри Task.Run, чтобы не блокировать вызывающий поток.
                    await Task.Run(() => OpenPortOnce(key, options), ct);
                    return;
                }
                catch (Exception ex) when (IsPortUnavailableException(ex))
                {
                    lastError = ex;
                    if (sw.Elapsed >= totalTimeout)
                    {
                        throw new IOException($"Не удалось открыть порт {key.PortName} в течение отведённого времени.", ex);
                    }

                    var remaining = totalTimeout - sw.Elapsed;
                    var delay = remaining < retryDelay ? remaining : retryDelay;
                    if (delay <= TimeSpan.Zero)
                        delay = TimeSpan.FromMilliseconds(50);

                    await Task.Delay(delay, ct);
                }
            }
        }

        private void OpenPortOnce(PortKey key, SerialPortOptions options)
        {
            SerialPort? createdPort = null;
            try
            {
                var p = new SerialPort(key.PortName, key.BaudRate, key.Parity, key.DataBits, key.StopBits)
                {
                    ReadTimeout = options.ReadTimeoutMs,
                    WriteTimeout = options.WriteTimeoutMs,
                    RtsEnable = options.RtsEnable,
                    DtrEnable = options.DtrEnable
                };
                p.ReadBufferSize = Math.Max(p.ReadBufferSize, options.ReadBufferSize);
                p.WriteBufferSize = Math.Max(p.WriteBufferSize, options.WriteBufferSize);

                createdPort = p;
                p.Open();

                _port = p;
                _isOpen = true;
                createdPort = null; // ответственность передана _port
            }
            finally
            {
                if (createdPort is not null)
                {
                    try
                    {
                        createdPort.Dispose();
                    }
                    catch
                    {
                        // best effort cleanup
                    }
                }
            }
        }

        private static bool IsPortUnavailableException(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException;

        private bool PortRequiresReopen() => !_isOpen || _port is null || !_port.IsOpen;

        private async Task RecoverPortAsync(CancellationToken ct)
        {
            if (_options is null)
                throw new InvalidOperationException("Параметры последовательного порта не заданы.");

            await CloseAsync();
            await OpenWithRetriesAsync(_key, _options, ct);
        }

        #endregion
    }
}
