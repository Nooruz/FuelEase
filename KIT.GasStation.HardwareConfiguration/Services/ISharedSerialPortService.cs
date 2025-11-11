using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Высокоуровневый владелец конкретного COM-порта, обеспечивающий одновременной доступ многим клиентам через очередь.
    /// </summary>
    public interface ISharedSerialPortService : IAsyncDisposable
    {
        /// <summary>Признак, что порт открыт.</summary>
        bool IsOpen { get; }

        /// <summary>Имя порта (COM3 и т.п.).</summary>
        string PortName { get; }

        /// <summary>Открыть порт (идемпотентно).</summary>
        Task OpenAsync(PortKey key, SerialPortOptions options, CancellationToken ct);

        /// <summary>Закрыть порт (идемпотентно); обычно вызывается менеджером при отсутствии лизов.</summary>
        Task CloseAsync();

        /// <summary>
        /// Последовательно отправить и прочитать ровно N байт (с ретраями).
        /// Внутри встроен общий семафор, исключающий одновременный I/O.
        /// </summary>
        Task<byte[]> WriteReadAsync(
            byte[] tx,
            int expectedRxLength,
            int writeToReadDelayMs = 50,
            int readTimeoutMs = 3000,
            int maxRetries = 2,
            CancellationToken ct = default);
    }
}
