using FuelEase.ViewModels.Base;

namespace FuelEase.ViewModels.Settings
{
    public class IdentificationSettingsViewModel : BaseViewModel
    {
        #region Private Members



        #endregion

        #region Public Properties

        public string NameGasStation
        {
            get => Properties.Settings.Default.NameGasStation;
            set
            {
                Properties.Settings.Default.NameGasStation = value;
                Properties.Settings.Default.Save();
            }
        }

        public string IdGasStation
        {
            get => Properties.Settings.Default.IdGasStation;
            set
            {
                Properties.Settings.Default.IdGasStation = value;
                Properties.Settings.Default.Save();
            }
        }

        #endregion
    }
}
