using KIT.App.Infrastructure.Factories;
using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Exceptions;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Properties;
using KIT.GasStation.State.Users;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIT.GasStation.State.CashRegisters
{
    public class CashRegisterStore : ICashRegisterStore
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly ICashRegisterFactory _cashRegisterFactory;
        private ICashRegisterService _cashRegisterService;
        private CashRegister _cashRegister;
        private ObservableCollection<CashRegister> _cashRegisters = new();
        private readonly IUserStore _userStore;
        private CashRegisterStatus _status;
        private DateTime? _openAt;
        private DateTime? _closeAt;
        private ShiftSalesReport? _shiftSalesReport;

        #endregion

        #region Public Properties

        public CashRegister CashRegister
        {
            get => _cashRegister;
            private set
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
        public CashRegisterStatus Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        public DateTime? OpenAt
        {
            get => _openAt;
            set
            {
                _openAt = value;
                OnPropertyChanged(nameof(OpenAt));
            }
        }
        public DateTime? CloseAt
        {
            get => _closeAt;
            set
            {
                _closeAt = value;
                OnPropertyChanged(nameof(CloseAt));
            }
        }

        public ShiftSalesReport? ShiftSalesReport
        {
            get => _shiftSalesReport;
            private set
            {
                _shiftSalesReport = value;
                OnPropertyChanged(nameof(ShiftSalesReport));
            }
        }

        #endregion

        #region Constructors

        public CashRegisterStore(IHardwareConfigurationService hardwareConfigurationService,
            ICashRegisterFactory cashRegisterFactory,
            IUserStore userStore)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _cashRegisterFactory = cashRegisterFactory;
            _userStore = userStore;
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
            await _cashRegisterService.OpenShiftAsync(_userStore.CurrentUser.FullName);
            // Обновляем отчёт по смене после каждой продажи
            _ = Task.Run(async () =>
            {
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
            });
            await GetShiftStateAsync();
        }

        public async Task CloseShiftAsync()
        {
            await _cashRegisterService.CloseShiftAsync(_userStore.CurrentUser.FullName);
            await GetShiftStateAsync();
        }

        public async Task XReportAsync()
        {
            // Обновляем отчёт по смене после каждой продажи
            _ = Task.Run(async () =>
            {
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
            });
            await _cashRegisterService.XReportAsync();
        }

        public async Task<CashRegisterState> GetShiftStateAsync()
        {
            var state = await _cashRegisterService.GetShiftStateAsync();
            Status = state.Status;
            OpenAt = state.OpenedAt;
            // Обновляем отчёт по смене после каждой продажи
            _ = Task.Run(async () =>
            {
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
            });
            return state;
        }

        public async Task<ShiftSalesReport> GetShiftSalesReportAsync()
        {
            var report = await _cashRegisterService.GetShiftSalesReportAsync();
            ShiftSalesReport = report;
            return report;
        }

        public async Task<FiscalData?> SaleAsync(FiscalData fiscalData)
        {
            var result = await _cashRegisterService.SaleAsync(fiscalData, _userStore.CurrentUser.FullName);

            // Обновляем отчёт по смене после каждой продажи
            _ = Task.Run(async () =>
            {
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
            });

            return result;
        }

        public async Task<FiscalData?> ReturnAsync(FiscalData fiscalData)
        {
            var result = await _cashRegisterService.ReturnAsync(fiscalData);

            // Обновляем отчёт по смене после каждой продажи
            _ = Task.Run(async () =>
            {
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
            });

            return result;
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

                await _cashRegisterService.InitializationAsync(CashRegister.Id);

                await GetShiftStateAsync();

                // Получаем отчёт по продажам за смену при запуске
                try { await GetShiftSalesReportAsync(); } catch { /* не критично */ }
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
