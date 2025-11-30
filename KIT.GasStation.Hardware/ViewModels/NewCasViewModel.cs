using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Common.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class NewCasViewModel : BaseViewModel
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly ICashRegisterFactory _cashRegisterFactory;
        private CashRegister _selectedCashRegister;
        private List<TapeType> _tapeTypes = Enum.GetValues(typeof(TapeType)).Cast<TapeType>().ToList();

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
        public List<string> Printers { get; set; }
        public List<TapeType> TapeTypes
        {
            get => _tapeTypes;
            set
            {
                _tapeTypes = value;
                OnPropertyChanged(nameof(TapeTypes));
            }
        }

        #endregion

        #region Constructor

        public NewCasViewModel(IHardwareConfigurationService hardwareConfigurationService,
            ICashRegisterFactory cashRegisterFactory)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _cashRegisterFactory = cashRegisterFactory;

            GetData();
        }

        #endregion

        #region Public Voids

        [Command]
        public void Check()
        {

        }

        [Command]
        public async Task Save()
        {
            if (ValidateRequiredFields())
            {
                await _hardwareConfigurationService.SaveCashRegisterAsync(SelectedCashRegister);
            }
        }

        #endregion

        #region Private Voids

        private void GetData()
        {
            try
            {
                Printers = new()
                {
                    "Не задан"
                };
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    Printers.Add(printer);
                }
            }
            catch (Exception e)
            {
                //_logger.LogError(e, e.Message);
            }
        }

        private bool ValidateRequiredFields()
        {
            if (string.IsNullOrEmpty(SelectedCashRegister.Address))
            {
                ShowErrorMessage("Введите адрес!");
                return false;
            }
            //if (string.IsNullOrEmpty(SelectedCashRegister.RegistrationNumber))
            //{
            //    ShowErrorMessage("Введите регистрационный номер устройства!");
            //    return false;
            //}
            //if (string.IsNullOrEmpty(SelectedCashRegister.UserName))
            //{
            //    ShowErrorMessage("Введите пользователя!");
            //    return false;
            //}
            //if (string.IsNullOrEmpty(SelectedCashRegister.Password))
            //{
            //    ShowErrorMessage("Введите пароль!");
            //    return false;
            //}
            if (!Uri.IsWellFormedUriString(SelectedCashRegister.Address, UriKind.Absolute))
            {
                ShowErrorMessage("Адрес неправильно заполнен!");
                return false;
            }
            return true;
        }

        private void ShowErrorMessage(string message)
        {
            _ = MessageBoxService.ShowMessage(message, "Внимание", MessageButton.OK, MessageIcon.Exclamation);
        }

        #endregion
    }
}
