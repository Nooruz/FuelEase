using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.HardwareConfigurations.Models;
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

        public CashRegisterStateInfoViewModel(CashRegisterState state)
        {
            switch (state.Status)
            {
                case CashRegisterStatus.Unknown:
                    ShiftState = "Смена ККМ: неизвестен статус";
                    break;
                case CashRegisterStatus.Open:
                    ShiftState = $"Смена ККМ: открыта";
                    break;
                case CashRegisterStatus.Close:
                    ShiftState = $"Смена ККМ: закрыта";
                    break;
                case CashRegisterStatus.Exceeded24Hours:
                    ShiftState = $"Смена ККМ: превышено 24 часа с момента открытия";
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
