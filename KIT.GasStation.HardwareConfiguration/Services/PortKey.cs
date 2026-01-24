using System.IO.Ports;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Ключ порта (включает ВСЕ значимые параметры линии).
    /// </summary>
    public readonly struct PortKey : IEquatable<PortKey>
    {
        public string PortName { get; }
        public int BaudRate { get; }
        public Parity Parity { get; }
        public int DataBits { get; }
        public StopBits StopBits { get; }
        public Handshake Handshake { get; } = Handshake.None;

        public PortKey(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake = Handshake.None)
        {
            PortName = Normalize(portName);
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            Handshake = handshake;
        }

        public static string Normalize(string portName) => portName?.Trim().ToUpperInvariant() ?? throw new ArgumentNullException(nameof(portName));

        public bool Equals(PortKey other)
            => PortName == other.PortName &&
               BaudRate == other.BaudRate &&
               Parity == other.Parity &&
               DataBits == other.DataBits &&
               StopBits == other.StopBits &&
               Handshake == other.Handshake;

        public override bool Equals(object? obj) => obj is PortKey k && Equals(k);

        public override int GetHashCode()
            => HashCode.Combine(PortName, BaudRate, Parity, DataBits, StopBits, Handshake);

        public override string ToString() => $"{PortName}@{BaudRate},{DataBits}{Parity}{(int)StopBits}";
    }
}
