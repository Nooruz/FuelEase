using KIT.GasStation.HardwareSettings.CustomControl.Views;
namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class KITPage : IPage
    {
        private readonly KITView _view;
        public KITPage(KITView view) { _view = view; }
        public Control View => _view;
        public void OnShow() { }
        public void Dispose() { _view.Dispose(); }
    }
}
