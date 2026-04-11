using KIT.GasStation.HardwareSettings.CustomControl.Views;
namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class PKElectronicsPage : IPage
    {
        private readonly PKElectronicsView _view;
        public PKElectronicsPage(PKElectronicsView view) { _view = view; }
        public Control View => _view;
        public void OnShow() { }
        public void Dispose() { _view.Dispose(); }
    }
}
