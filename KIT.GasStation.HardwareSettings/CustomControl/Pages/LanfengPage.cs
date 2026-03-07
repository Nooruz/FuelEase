using KIT.GasStation.HardwareSettings.CustomControl.Views;

namespace KIT.GasStation.HardwareSettings.CustomControl.Pages
{
    public sealed class LanfengPage : IPage
    {
        private readonly LanfengView _view;

        public LanfengPage(LanfengView view)
        {
            _view = view;
        }

        public Control View => _view;

        public void OnShow()
        {
            // например обновить данные, подписаться на события и т.п.
        }

        public void Dispose()
        {
            _view.Dispose();
        }
    }
}
