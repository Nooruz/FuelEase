using KIT.GasStation.HardwareSettings.CustomControl.Views;
namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class EKassaPage : IPage
    {
        private readonly EKassaView _view;
        public EKassaPage(EKassaView view) { _view = view; }
        public Control View => _view;
        public void OnShow() { }
        public void Dispose() { _view.Dispose(); }
    }
}
