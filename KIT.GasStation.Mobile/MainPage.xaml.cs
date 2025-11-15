using KIT.GasStation.Mobile.ViewModels;

namespace KIT.GasStation.Mobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }
    }

}
