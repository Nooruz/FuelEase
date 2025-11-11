using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Опции открытия последовательного порта.
    /// </summary>
    public sealed record SerialPortOptions(
        int BaudRate,
        Parity Parity,
        int DataBits,
        StopBits StopBits,
        bool RtsEnable = false,
        bool DtrEnable = false,
        int ReadTimeoutMs = 3000,
        int WriteTimeoutMs = 1000,
        int ReadBufferSize = 64 * 1024,
        int WriteBufferSize = 64 * 1024
    );
}
