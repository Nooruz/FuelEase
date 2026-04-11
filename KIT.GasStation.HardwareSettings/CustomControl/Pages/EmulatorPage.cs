using KIT.GasStation.HardwareSettings.CustomControl.Views;
namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class EmulatorPage : IPage
    {
        private readonly EmulatorView _view;
        public EmulatorPage(EmulatorView view) { _view = view; }
        public Control View => _view;
        public void OnShow() { }
        public void Dispose() { _view.Dispose(); }
    }
}
