using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class UncompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<UncompletedSalesViewModel> _logger;
        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFiscalDataService _fiscalDataService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IUserStore _userStore;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private ObservableCollection<FuelSale> _selectedFuelSales = new();
        private FuelSale _selectedFuelSale;
        private FiscalData _selectedFiscalData;
        private bool _showFuelSaleLoadingPanel;
        private bool _showFiscalDataLoadingPanel;

        #endregion

        #region Public Properties

        public ObservableCollection<FuelSale> FuelSales
        {
            get => _fuelSales;
            set
            {
                _fuelSales = value;
                OnPropertyChanged(nameof(FuelSales));
            }
        }
        public ObservableCollection<FuelSale> SelectedFuelSales
        {
            get => _selectedFuelSales;
            set
            {
                _selectedFuelSales = value;
                OnPropertyChanged(nameof(SelectedFuelSales));
            }
        }
        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public List<KeyValuePair<FuelSaleStatus, string>> FuelSaleStatuses => new(EnumHelper.GetLocalizedEnumValues<FuelSaleStatus>());
        public bool ShowFuelSaleLoadingPanel
        {
            get => _showFuelSaleLoadingPanel;
            set
            {
                _showFuelSaleLoadingPanel = value;
                OnPropertyChanged(nameof(ShowFuelSaleLoadingPanel));
            }
        }
        public bool ShowFiscalDataLoadingPanel
        {
            get => _showFiscalDataLoadingPanel;
            set
            {
                _showFiscalDataLoadingPanel = value;
                OnPropertyChanged(nameof(ShowFiscalDataLoadingPanel));
            }
        }
        public FuelSale SelectedFuelSale
        {
            get => _selectedFuelSale;
            set
            {
                _selectedFuelSale = value;
                OnPropertyChanged(nameof(SelectedFuelSale));
                OnPropertyChanged(nameof(PrintReceiptTitle));
                OnPropertyChanged(nameof(IsVisiblePrintReceipt));
            }
        }
        public FiscalData SelectedFiscalData
        {
            get => _selectedFiscalData;
            set
            {
                _selectedFiscalData = value;
                OnPropertyChanged(nameof(SelectedFiscalData));
            }
        }
        public string PrintReceiptTitle
        {
            get
            {
                if (SelectedFuelSale?.ReceivedSum > 0)
                {
                    return $"Создать чек на {SelectedFuelSale.ReceivedSum:N2} сом";
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public bool IsVisiblePrintReceipt
        {
            get
            {
                if (SelectedFuelSale?.ReceivedSum > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public List<KeyValuePair<OperationType, string>> OperationTypes => new(EnumHelper.GetLocalizedEnumValues<OperationType>());

        #endregion

        #region Constructor

        public UncompletedSalesViewModel(ILogger<UncompletedSalesViewModel> logger,
            IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore,
            IUserStore userStore,
            IFiscalDataService fiscalDataService)
        {
            _logger = logger;
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
            _userStore = userStore;
            _fiscalDataService = fiscalDataService;

            _fiscalDataService.OnCreated += FiscalDataService_OnCreated;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
        }

        #endregion

        #region Public Voids

        /// <summary>
        /// Завершить
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CompleteFuelSale()
        {
            try
            {
                if (!SelectedFuelSales.Any())
                    return;

                var result = MessageBoxService.ShowMessage(
                    "Завершить выбранных продаж?",
                    "Завершение",
                    MessageButton.YesNoCancel);

                if (result != MessageResult.Yes)
                    return;

                ShowFuelSaleLoadingPanel = true;

                var selectedItems = SelectedFuelSales.ToList();

                foreach (var item in selectedItems)
                {
                    item.FuelSaleStatus = FuelSaleStatus.Completed;
                    await _fuelSaleService.UpdateAsync(item.Id, item);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK);
            }
            finally
            {
                ShowFuelSaleLoadingPanel = false;
            }
        }

        /// <summary>
        /// Продолжить
        /// </summary>
        /// <returns></returns>
        [Command]
        public void ContinueFueling()
        {
            if (SelectedFuelSales.Any())
            {
                foreach (var item in SelectedFuelSales)
                {
                    _fuelSaleService.ResumeFueling(item);
                }
            }
        }

        [Command]
        public async Task Return()
        {
            try
            {
                if (SelectedFiscalData == null)
                {
                    MessageBoxService.ShowMessage("Выберите фискальный чек (ККМ) для возврата", "Ошибка", MessageButton.OK);
                    return;
                }

                var result = MessageBoxService.ShowMessage("Сделать возврат ККМ?", "Завершение", MessageButton.YesNoCancel);

                if (result == MessageResult.Cancel)
                {
                    return;
                }

                ShowFiscalDataLoadingPanel = true;

                if (result == MessageResult.Yes)
                {
                    var fiscalData = SelectedFiscalData.CreateReturnFiscalData();
                    var newFiscalData = await _cashRegisterStore.ReturnAsync(fiscalData);
                    if (newFiscalData != null)
                    {
                        await _fiscalDataService.CreateAsync(newFiscalData);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK);
            }
            finally
            {
                ShowFiscalDataLoadingPanel = false;
            }
        }

        [Command]
        public async Task PrintReceipt()
        {
            try
            {
                var result = MessageBoxService.ShowMessage($"Создать чек на {SelectedFuelSale.ReceivedSum} сом?", "Создание чека", MessageButton.YesNo);

                if (result == MessageResult.No)
                {
                    return;
                }

                ShowFuelSaleLoadingPanel = true;

                var fiscalData = SelectedFuelSale.CreateReceivedFiscalData();

                var newFiscalData = await _cashRegisterStore.SaleAsync(fiscalData);

                if (newFiscalData != null)
                {
                    await _fiscalDataService.CreateAsync(newFiscalData);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK);
            }
            finally
            {
                ShowFuelSaleLoadingPanel = false;
            }
        }

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            try
            {
                FuelSales = new(await _fuelSaleService.GetUncompletedFuelSaleAsync(_shiftStore.CurrentShift.Id));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void FiscalDataService_OnCreated(FiscalData fiscalData)
        {
            try
            {
                var fuelSale = FuelSales.FirstOrDefault(x => x.Id == fiscalData.FuelSaleId);

                if (fuelSale == null)
                    return;

                fuelSale.FiscalDatas.Add(fiscalData);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, e.Message);
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            try
            {
                if (fuelSale.FuelSaleStatus != FuelSaleStatus.Completed)
                    return;

                var existingFuelSale = FuelSales.FirstOrDefault(x => x.Id == fuelSale.Id);
                if (existingFuelSale == null)
                    return;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    FuelSales.Remove(existingFuelSale);
                });
            }
            catch (Exception e)
            {
                _logger?.LogError(e, e.Message);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }
}
