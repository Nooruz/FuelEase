namespace KIT.GasStation.HardwareSettings.CustomControl
{
    public interface IPage : IDisposable
    {
        Control View { get; }
        void OnShow(); // опционально
    }
}
