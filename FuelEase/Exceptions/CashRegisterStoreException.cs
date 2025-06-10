using System;

namespace FuelEase.Exceptions
{
    public class CashRegisterStoreException : Exception
    {
        public CashRegisterStoreException()
        {
        }

        public CashRegisterStoreException(string message) : base(message)
        {
        }

        public CashRegisterStoreException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
