using KIT.GasStation.ViewModels.Base;

namespace KIT.GasStation.ViewModels.Info
{
    public class CashRegisterStateInfoViewModel : BaseViewModel
    {
        #region Private Members

        private string? _shiftState = string.Empty;

        #endregion

        #region Public Properties

        public string? ShiftState
        {
            get => _shiftState;
            set
            {
                _shiftState = value;
                OnPropertyChanged(nameof(ShiftState));
            }
        }

        #endregion

        #region Constructors

        public CashRegisterStateInfoViewModel(string? shiftState)
        {
            ShiftState = shiftState;
        }

        #endregion
    }
}
