namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Опции открытия последовательного порта.
    /// </summary>
    public sealed record SerialPortOptions(
        bool RtsEnable = false,
        bool DtrEnable = false,
        int ReadTimeoutMs = 3000,
        int WriteTimeoutMs = 1000,
        int ReadBufferSize = 64 * 1024,
        int WriteBufferSize = 64 * 1024,
        int OpenRetryTimeoutMs = 30_000,
        int OpenRetryDelayMs = 1_000
    );
}
