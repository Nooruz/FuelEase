namespace KIT.GasStation.HardwareConfigurations.Services
{
    public interface IDevice<TType> where TType : Enum
    {
        string Name { get; set; }
        TType Type { get; set; }
    }
}
