namespace KIT.GasStation.CashRegisters.Exceptions
{
    public class CashRegisterException : Exception
    {
        public CashRegisterException()
        {
        }

        public CashRegisterException(string message) : base(message)
        {
        }

        public CashRegisterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
