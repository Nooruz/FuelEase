namespace KIT.GasStation.TechnoProjekt.Utilities
{
    public class ChecksumHelper
    {
        /// <summary>
        /// CRC = XOR всех байт от byteStart до byteEnd включительно.
        /// </summary>
        public static byte Calculate(byte[] data, int byteStart, int byteEnd)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            byte crc = 0;
            for (int i = byteStart; i <= byteEnd && i < data.Length; i++)
                crc ^= data[i];
            return crc;
        }
    }
}
