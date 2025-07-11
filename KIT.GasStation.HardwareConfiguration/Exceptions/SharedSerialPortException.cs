namespace KIT.GasStation.HardwareConfigurations.Exceptions
{
    public class SharedSerialPortException : Exception
    {
        public SharedSerialPortException()
        {
        }
        public SharedSerialPortException(string message) : base(message)
        {
        }

        public SharedSerialPortException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
