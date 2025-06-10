namespace FuelEase.Lanfeng.Utilities
{
    /// <summary>
    /// Предоставляет методы для преобразования данных в массивы байтов и обратно.
    /// </summary>
    public static class ByteConverter
    {
        /// <summary>
        /// Преобразует десятичное значение в массив байтов.
        /// </summary>
        /// <param name="value">Десятичное значение для преобразования.</param>
        /// <param name="byteCount">Количество байтов в результате.</param>
        /// <returns>Массив байтов, представляющий значение.</returns>
        public static byte[] ConvertDecimalToBytes(decimal value, int byteCount = 4)
        {
            var intValue = (int)(value * 100);
            var bytes = BitConverter.GetBytes(intValue);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            var result = bytes.Take(byteCount).ToArray();

            string sd = BitConverter.ToString(result);

            return bytes.Take(byteCount).ToArray();
        }

        /// <summary>
        /// Преобразует массив байтов в десятичное значение.
        /// </summary>
        /// <param name="bytes">Массив байтов для преобразования.</param>
        /// <returns>Десятичное значение.</returns>
        public static decimal ConvertBytesToDecimal(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            var intValue = BitConverter.ToInt32(bytes, 0);
            return intValue / 100m;
        }

        /// <summary>
        /// Объединяет несколько массивов байтов в один массив.
        /// </summary>
        /// <param name="arrays">Массивы байтов для объединения.</param>
        /// <returns>Объединенный массив байтов.</returns>
        public static byte[] CombineBytes(params byte[][] arrays)
        {
            var combined = new List<byte>();
            foreach (var array in arrays)
            {
                combined.AddRange(array);
            }
            return combined.ToArray();
        }
    }
}
