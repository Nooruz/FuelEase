using System.Text;

namespace KIT.GasStation.EKassa.Helpers
{
    public static class EscPos
    {
        private static readonly Encoding Enc = Encoding.GetEncoding(866); // часто для чеков норм (CP866)
                                                                          // если у тебя всё UTF-8 — можно заменить на Encoding.UTF8, но не все Xprinter любят UTF-8

        public static void PrintReceiptWithQr(string printerName, string text, string qrData)
        {
            var b = new List<byte>();

            // init
            b.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @

            // текст
            b.AddRange(Enc.GetBytes(text));
            b.AddRange(new byte[] { 0x0A, 0x0A }); // LF LF

            // центр
            b.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1

            // QR: model 2
            b.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });

            // QR: размер модуля (1..16) -> 6-8 обычно отлично
            b.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x07 }); // 7

            // QR: уровень коррекции (48=L,49=M,50=Q,51=H)
            b.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31 }); // M

            // QR: store data
            byte[] qrBytes = Encoding.ASCII.GetBytes(qrData); // QR лучше в ASCII/UTF-8, ссылки норм
            int pL = (qrBytes.Length + 3) & 0xFF;
            int pH = (qrBytes.Length + 3) >> 8;

            b.AddRange(new byte[] { 0x1D, 0x28, 0x6B, (byte)pL, (byte)pH, 0x31, 0x50, 0x30 });
            b.AddRange(qrBytes);

            // QR: print
            b.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

            b.AddRange(new byte[] { 0x0A, 0x0A, 0x0A });

            // left align back
            b.AddRange(new byte[] { 0x1B, 0x61, 0x00 });

            // cut (если принтер умеет)
            b.AddRange(new byte[] { 0x1D, 0x56, 0x01 }); // GS V 1 (partial cut)

            RawPrinterHelper.SendBytes(printerName, b.ToArray());
        }
    }
}
