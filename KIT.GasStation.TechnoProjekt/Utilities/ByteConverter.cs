namespace KIT.GasStation.TechnoProjekt.Utilities
{
    public class ByteConverter
    {
        // Преобразование десятичного значения (сомы или литры) в ASCII-поле из 6 символов
        public static byte[] DecimalToSixAscii(decimal value, bool isPriceInRubles)
        {
            if (isPriceInRubles)
            {
                int kop = (int)Math.Round(value * 100m);
                var s = kop.ToString("D6");
                return s.Select(c => (byte)c).ToArray();
            }
            else
            {
                // если value в литрах => перевод в мл
                int ml = (int)Math.Round(value * 1000m);
                var s = ml.ToString("D6");
                return s.Select(c => (byte)c).ToArray();
            }
        }

        public static decimal SixAsciiToDecimalPrice(byte[] bytes)
        {
            var s = System.Text.Encoding.ASCII.GetString(bytes);
            if (!int.TryParse(s, out int kop)) return 0m;
            return kop / 100m;
        }

        public static decimal SixAsciiToDecimalVolume(byte[] bytes)
        {
            var s = System.Text.Encoding.ASCII.GetString(bytes);
            if (!int.TryParse(s, out int ml)) return 0m;
            return ml / 1000m;
        }
    }
}
