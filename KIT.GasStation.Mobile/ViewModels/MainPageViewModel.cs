using CommunityToolkit.Mvvm.ComponentModel;

namespace KIT.GasStation.Mobile.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        #region Private Members

        [ObservableProperty]
        private decimal _price = 74.4M;

        [ObservableProperty]
        private bool _lifted = true;

        #endregion
    }
}
