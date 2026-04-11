using KIT.GasStation.HardwareSettings.CustomControl.Views;
namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class NewCasPage : IPage
    {
        private readonly NewCasView _view;
        public NewCasPage(NewCasView view) { _view = view; }
        public Control View => _view;
        public void OnShow() { }
        public void Dispose() { _view.Dispose(); }
    }
}
