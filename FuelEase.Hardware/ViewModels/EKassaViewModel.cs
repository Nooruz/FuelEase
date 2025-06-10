using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.CashRegisters.Services;
using FuelEase.Common.Factories;
using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.Hardware.ViewModels
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

                cashRegisterService.OnShiftOpened += OnShiftOpened;
                cashRegisterService.OnStatusChanged += CashRegisterService_OnStatusChanged;

                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                string? message = await cashRegisterService.GetShiftStateAsync();

                File.WriteAllText(Path.Combine(@"D:\Чеки", $"чек_{Guid.NewGuid()}"), message);

                if (message != null)
                {
                    _ = MessageBoxService.ShowMessage(message, "Статус ККМ", MessageButton.OK, MessageIcon.Information);
                }
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

                cashRegisterService.OnShiftOpened += OnShiftOpened;
                cashRegisterService.OnStatusChanged += CashRegisterService_OnStatusChanged;

                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                await cashRegisterService.OpenShiftAsync();
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

                cashRegisterService.OnShiftClosed += OnShiftClosed;
                cashRegisterService.OnStatusChanged += CashRegisterService_OnStatusChanged;

                await cashRegisterService.InitializationAsync(SelectedCashRegister.Id);

                await cashRegisterService.CloseShiftAsync();
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

        private void OnShiftOpened()
        {
            MessageBoxService.ShowMessage("Смена открыта", "Информация", MessageButton.OK, MessageIcon.Information);
        }

        private void OnShiftClosed()
        {
            MessageBoxService.ShowMessage("Смена закрыта", "Информация", MessageButton.OK, MessageIcon.Information);
        }

        private void CashRegisterService_OnStatusChanged(CashRegisterStatus status)
        {
            string? message = status switch
            {
                CashRegisterStatus.Exceeded24Hours => "Смена на ККМ открыта более 24 часов. Пожалуйста, закройте смену и откройте новую.",
                CashRegisterStatus.Close => "Смена на ККМ закрыта. Пожалуйста, откройте новую смену перед началом работы.",
                CashRegisterStatus.Error => "Ошибка ККМ. Проверьте соединение с сервером или настройки кассы.",
                CashRegisterStatus.Unknown => "Статус ККМ неизвестен. Проверьте работу ККМ.",
                CashRegisterStatus.NoOpenedShift => "Смена на ККМ не открыта. Откройте смену перед началом работы.",
                _ => null
            };

            if (message != null)
            {
                _ = MessageBoxService.ShowMessage(message, "Внимание!", MessageButton.OK, MessageIcon.Warning);
            }
        }

        #endregion
    }
}
