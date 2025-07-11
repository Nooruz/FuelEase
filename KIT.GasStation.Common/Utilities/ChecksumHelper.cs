namespace KIT.GasStation.Common.Utilities
{
    /// <summary>
    /// Предоставляет методы для вычисления и проверки контрольных сумм.
    /// </summary>
    public static class ChecksumHelper
    {
        /// <summary>
        /// Вычисляет контрольную сумму для заданного массива данных.
        /// </summary>
        /// <param name="data">Данные для вычисления контрольной суммы.</param>
        /// <returns>Вычисленная контрольная сумма.</returns>
        public static byte CalculateChecksum(byte[] data)
        {
            return (byte)(data.Skip(1).Take(data.Length - 2).Aggregate(0, (current, b) => current - b) & 0xFF);
        }
    }
}
