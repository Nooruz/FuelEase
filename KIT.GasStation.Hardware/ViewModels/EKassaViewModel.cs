using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Common.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class EKassaViewModel : BaseViewModel
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

        public EKassaViewModel(IHardwareConfigurationService hardwareConfigurationService,
            ICashRegisterFactory cashRegisterFactory)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _cashRegisterFactory = cashRegisterFactory;

            GetData();
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task State()
        {
            try
            {
                // Сохраняем изменения
                await Save();

                ICashRegisterService cashRegisterService = await _cashRegisterFactory.CreateAsync(SelectedCashRegister.Id);

                if (cashRegisterService == null)
                {
                    MessageBoxService.ShowMessage("Не удалось получить статус ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                    return;
                }


                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                var state = await cashRegisterService.GetShiftStateAsync();

                //_ = MessageBoxService.ShowMessage(, "Статус ККМ", MessageButton.OK, MessageIcon.Information);
            }
            catch (CashRegisterException ex)
            {
                MessageBoxService.ShowMessage(ex.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
        }

        [Command]
        public async Task OpenShift()
        {
            try
            {
                // Сохраняем изменения
                await Save();

                ICashRegisterService cashRegisterService = await _cashRegisterFactory.CreateAsync(SelectedCashRegister.Id);

                if (cashRegisterService == null)
                {
                    MessageBoxService.ShowMessage("Не удалось открыть смену ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                    return;
                }


                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                await cashRegisterService.OpenShiftAsync("");
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
        }

        [Command]
        public async Task CloseShift()
        {
            try
            {
                // Сохраняем изменения
                await Save();


                ICashRegisterService cashRegisterService = await _cashRegisterFactory.CreateAsync(SelectedCashRegister.Id);

                if (cashRegisterService == null)
                {
                    MessageBoxService.ShowMessage("Не удалось закрыть смену ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                    return;
                }


                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                await cashRegisterService.CloseShiftAsync("");
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
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
            if (string.IsNullOrEmpty(SelectedCashRegister.RegistrationNumber))
            {
                ShowErrorMessage("Введите регистрационный номер устройства!");
                return false;
            }
            if (string.IsNullOrEmpty(SelectedCashRegister.UserName))
            {
                ShowErrorMessage("Введите пользователя!");
                return false;
            }
            if (string.IsNullOrEmpty(SelectedCashRegister.Password))
            {
                ShowErrorMessage("Введите пароль!");
                return false;
            }
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
