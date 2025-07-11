namespace KIT.GasStation.HardwareConfigurations.Services
{
    public interface IPortManager
    {
        /// <summary>
        /// Получить сервис, работающий с указанным портом (открывает при необходимости).
        /// </summary>
        /// <param name="portName">Имя порта (например, "COM3").</param>
        /// <param name="baudRate">Скорость порта (например, 9600).</param>
        /// <returns>Экземпляр сервиса, обеспечивающего работу с выбранным портом.</returns>
        Task<ISharedSerialPortService> GetPortServiceAsync(string portName, int baudRate);

        /// <summary>
        /// Явно закрыть и освободить указанный порт.
        /// </summary>
        /// <param name="portName">Имя порта (например, "COM3").</param>
        /// <param name="baudRate">Скорость, с которой открывался порт.</param>
        void ClosePortService(string portName, int baudRate);
    }
}
