using Microsoft.Extensions.Logging;
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

        #endregion

        #region Public Properties

        public bool IsOpen => _isOpen;
        public string PortName => _key.PortName;

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
            await Task.Run(() =>
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

                p.Open();
                _port = p;
                _isOpen = true;
            }, ct);
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
            if (!_isOpen || _port is null || !_port.IsOpen)
                throw new InvalidOperationException("Порт не открыт.");

            await _io.WaitAsync(ct);
            try
            {
                // Простой retry-цикл: если таймаут/IO — ещё попытки (maxRetries).
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        // === WRITE ===
                        await _port.BaseStream.WriteAsync(tx, 0, tx.Length, ct);
                        await _port.BaseStream.FlushAsync(ct);

                        if (writeToReadDelayMs > 0)
                            await Task.Delay(writeToReadDelayMs, ct);

                        // === READ EXACTLY expectedRxLength ===
                        var buf = new byte[expectedRxLength];
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var total = 0;

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
                                continue;
                            }
                            // n == 0 — просто ждём дальше до дедлайна
                        }

                        // подчистим возможные лишние байты
                        if (_port.BytesToRead > 0) _port.DiscardInBuffer();

                        return buf;
                    }
                    catch (TimeoutException) when (attempt <= maxRetries)
                    {
                        // небольшой экспоненциальный бэкофф
                        await Task.Delay(50 * attempt, ct);
                        continue;
                    }
                    catch (IOException) when (attempt <= maxRetries)
                    {
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
    }
}
