using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Интерфейс для управления состоянием COM-порта.
    /// </summary>
    public interface ISharedSerialPortService : IDisposable
    {
        SerialPort Port { get; }

        /// <summary>
        /// Событие при получении данных.
        /// </summary>
        event Action<byte[]> OnDataReceived;

        /// <summary>
        /// Открытие порта.
        /// </summary>
        /// <param name="portName">Имя порта.</param>
        /// <param name="baudRate">Скорость передачи данных в бодах.</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию открытия порта.</returns>
        Task OpenAsync(string portName, int baudRate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Закрытие порта.
        /// </summary>
        void Close();

        /// <summary>
        /// Запись данных в порт.
        /// </summary>
        /// <param name="bytes">Данные для записи.</param>
        /// <param name="expectedResponseLength">Ожидаемая длина ответа.</param>
        /// <param name="maxRetries">Количество попыток в случае неудачи.</param>
        /// <param name="writeTimeout">Тайм-аут записи в миллисекундах.</param>
        Task<byte[]> WriteReadAsync(byte[] bytes,
            int readBufferLength,
            int maxRetries = 3,
            int writeToReadDelayMs = 50,
            int readTimeoutMs = 3000);
    }
}
