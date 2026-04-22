using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
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
    public class CompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<CompletedSalesViewModel> _logger;
        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IFiscalDataService _fiscalDataService;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private FuelSale _selectedFuelSale;
        private FiscalData _selectedFiscalData;
        private bool _showFuelSaleLoadingPanel;
        private bool _showFiscalDataLoadingPanel;

        #endregion

        #region Public Properties

        public List<KeyValuePair<OperationType, string>> OperationTypes => new(EnumHelper.GetLocalizedEnumValues<OperationType>());
        public ObservableCollection<FuelSale> FuelSales
        {
            get => _fuelSales;
            set
            {
                _fuelSales = value;
                OnPropertyChanged(nameof(FuelSales));
            }
        }

        public FuelSale SelectedFuelSale
        {
            get => _selectedFuelSale;
            set
            {
                _selectedFuelSale = value;
                OnPropertyChanged(nameof(SelectedFuelSale));
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

        #endregion

        #region Constructor

        public CompletedSalesViewModel(IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore,
            IFiscalDataService fiscalDataService,
            ILogger<CompletedSalesViewModel> logger)
        {
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
            _fiscalDataService = fiscalDataService;
            _logger = logger;

            _fiscalDataService.OnCreated += FiscalDataService_OnCreated;
        }

        #endregion

        #region Public Voids

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
        public async Task PrintSaleReceipt()
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

        private async Task GetData()
        {
            FuelSales = new(await _fuelSaleService.GetCompletedFuelSaleAsync(_shiftStore.CurrentShift.Id));
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
