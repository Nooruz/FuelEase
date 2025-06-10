using FuelEase.CashRegisters.Services;
using FuelEase.Common.Factories;
using FuelEase.Domain.Models;
using FuelEase.Exceptions;
using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;
using FuelEase.Properties;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FuelEase.State.CashRegisters
{
    public class CashRegisterStore : ICashRegisterStore
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly ICashRegisterFactory _cashRegisterFactory;
        private ICashRegisterService _cashRegisterService;
        private CashRegister _cashRegister;
        private ObservableCollection<CashRegister> _cashRegisters = new();

        #endregion

        #region Actions

        public event Action OnReceiptPrinting;
        public event Action OnShiftOpened;
        public event Action OnShiftClosed;
        public event Action<FuelSale> OnReturning;
        public event Action<string> OnUnknownError;
        public event Action<CashRegisterStatus> OnStatusChanged;

        #endregion

        #region Public Properties

        public CashRegister CashRegister
        {
            get => _cashRegister;
            set
            {
                _cashRegister = value;
                OnPropertyChanged(nameof(CashRegister));
            }
        }
        public ObservableCollection<CashRegister> CashRegisters
        {
            get => _cashRegisters;
            set
            {
                _cashRegisters = value;
                OnPropertyChanged(nameof(CashRegisters));
            }
        }

        #endregion

        #region Constructors

        public CashRegisterStore(IHardwareConfigurationService hardwareConfigurationService,
            ICashRegisterFactory cashRegisterFactory)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _cashRegisterFactory = cashRegisterFactory;
        }

        #endregion

        #region Public Voids

        public void ChangeDefaultCashRegister(Guid guid)
        {
            Settings.Default.DefaultCashRegisterId = guid;
            Settings.Default.Save();
        }

        public async Task OpenShiftAsync()
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.OpenShiftAsync();
        }

        public async Task CloseShiftAsync()
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.CloseShiftAsync();
        }

        public async Task XReportAsync()
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.XReportAsync();
        }

        public async Task<string?> GetShiftStateAsync()
        {
            if (_cashRegisterService == null)
            {
                return string.Empty;
            }

            return await _cashRegisterService.GetShiftStateAsync();
        }

        public async Task SaleAsync(FuelSale fuelSale, Fuel fuel)
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.SaleAsync(fuelSale, fuel);
        }

        public async Task ReturnAsync(FuelSale fuelSale, Fuel fuel)
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.ReturnAsync(fuelSale, fuel);
        }

        public async Task ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel)
        {
            if (_cashRegisterService == null)
            {
                return;
            }

            await _cashRegisterService.ReturnAndReceivedSaleAsync(fuelSale, fuel);
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Получить кассу по умолчанию
        /// </summary>
        private void GetDefaultCashRegister()
        {
            try
            {
                // Получаем идентификатор кассы по умолчанию
                Guid defaultCashRegisterId = Settings.Default.DefaultCashRegisterId;

                // Получаем кассу по идентификатору
                CashRegister? cashRegister = CashRegisters.FirstOrDefault(c => c.Id == defaultCashRegisterId);

                // Если касса не найдена, то получаем первую кассу из списка
                cashRegister ??= CashRegisters.FirstOrDefault();

                if (cashRegister == null)
                {
                    throw new CashRegisterStoreException("Не найдено ни одной кассы");
                }

                CashRegister = cashRegister;
                Settings.Default.DefaultCashRegisterId = cashRegister.Id;
                Settings.Default.Save();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// Получить список касс
        /// </summary>
        private async Task GetCashRegisters()
        {
            try
            {
                CashRegisters = await _hardwareConfigurationService.GetCashRegistersAsync();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void CashRegisterService_OnReceiptPrinting()
        {
            OnReceiptPrinting?.Invoke();
        }

        private void CashRegisterService_OnStatusChanged(CashRegisterStatus status)
        {
            CashRegister.Status = status;
            OnStatusChanged?.Invoke(status);
        }

        private void CashRegisterService_OnShiftClosed()
        {
            OnShiftClosed?.Invoke();
        }

        private void CashRegisterService_OnUnknownError(string errorMessage)
        {
            OnUnknownError?.Invoke(errorMessage);
        }

        private void CashRegisterService_OnShiftOpened()
        {
            OnShiftOpened?.Invoke();
        }

        private void CashRegisterService_OnReturning(FuelSale fuelSale)
        {
            OnReturning?.Invoke(fuelSale);
        }

        #endregion

        #region HostedService

        /// <summary>
        /// Запуск сервиса
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Получаем список касс
            await GetCashRegisters();

            // Получаем кассу по умолчанию
            GetDefaultCashRegister();

            if (CashRegister != null)
            {
                _cashRegisterService = await _cashRegisterFactory.CreateAsync(CashRegister.Id);

                _cashRegisterService.OnReceiptPrinting += CashRegisterService_OnReceiptPrinting;
                _cashRegisterService.OnStatusChanged += CashRegisterService_OnStatusChanged;
                _cashRegisterService.OnShiftOpened += CashRegisterService_OnShiftOpened;
                _cashRegisterService.OnReturning += CashRegisterService_OnReturning;
                _cashRegisterService.OnShiftClosed += CashRegisterService_OnShiftClosed;
                _cashRegisterService.OnUnknownError += CashRegisterService_OnUnknownError;

                await _cashRegisterService.InitializationAsync(CashRegister.Id);

                await _cashRegisterService.XReportAsync(false);

                return;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
