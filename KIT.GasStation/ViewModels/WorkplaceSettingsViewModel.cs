using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.ViewModels.Base;
using System.Collections.ObjectModel;

namespace KIT.GasStation.ViewModels
{
    public class WorkplaceSettingsViewModel : BaseViewModel
    {
        #region Private Members

        private readonly ICashRegisterStore _cashRegisterStore;
        private CashRegister _selectedCashRegister;

        #endregion

        #region Public Properties

        public CashRegister SelectedCashRegister
        {
            get => _selectedCashRegister;
            set
            {
                _selectedCashRegister = value;
                OnPropertyChanged(nameof(SelectedCashRegister));
            }
        }
        public ObservableCollection<CashRegister> CashRegisters => _cashRegisterStore.CashRegisters;

        #endregion

        #region Constructors

        public WorkplaceSettingsViewModel(ICashRegisterStore cashRegisterStore)
        {
            _cashRegisterStore = cashRegisterStore;
            _selectedCashRegister = _cashRegisterStore.CashRegister;
        }

        #endregion

        #region Public Voids

        [Command]
        public void Save()
        {
            if (SelectedCashRegister != null)
            {
                _cashRegisterStore.ChangeDefaultCashRegister(SelectedCashRegister.Id);
                
            }

            CurrentWindowService?.Close();
        }

        #endregion
    }
}
