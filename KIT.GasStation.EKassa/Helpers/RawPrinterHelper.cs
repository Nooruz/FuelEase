using System.Runtime.InteropServices;

namespace KIT.GasStation.EKassa.Helpers
{
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static void SendBytes(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                throw new InvalidOperationException("OpenPrinter failed.");

            try
            {
                var di = new DOCINFOA
                {
                    pDocName = "ESC/POS",
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                    throw new InvalidOperationException("StartDocPrinter failed.");

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new InvalidOperationException("StartPagePrinter failed.");

                    try
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocHGlobal(bytes.Length);
                        try
                        {
                            Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                            if (!WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int written) || written != bytes.Length)
                                throw new InvalidOperationException("WritePrinter failed.");
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pUnmanagedBytes);
                        }
                    }
                    finally
                    {
                        EndPagePrinter(hPrinter);
                    }
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
