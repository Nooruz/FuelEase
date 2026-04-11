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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class CompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IFiscalDataService _fiscalDataService;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private FuelSale _selectedFuelSale;
        private FiscalData _selectedFiscalData;
        private bool _showLoadingPanel;

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

        public bool ShowLoadingPanel
        {
            get => _showLoadingPanel;
            set
            {
                _showLoadingPanel = value;
                OnPropertyChanged(nameof(ShowLoadingPanel));
            }
        }

        #endregion

        #region Constructor

        public CompletedSalesViewModel(IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore,
            IFiscalDataService fiscalDataService)
        {
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
            _fiscalDataService = fiscalDataService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Return()
        {
            if (SelectedFiscalData == null)
            {
                MessageBoxService.ShowMessage("Для возврата необходимо сначала выбрать чек.", 
                    "Выборерите чек",
                    MessageButton.OK,
                    MessageIcon.Warning);
                return;
            }

            var returnedFiscalData = SelectedFuelSale.FiscalDatas.FirstOrDefault(fd => fd.OperationType == OperationType.Return);

            if (returnedFiscalData != null)
            {
                var saleSum = SelectedFuelSale.FiscalDatas.Where(fd => fd.OperationType == OperationType.Sale).Sum(fd => fd.Total);
                var returnSum = SelectedFuelSale.FiscalDatas.Where(fd => fd.OperationType == OperationType.Return).Sum(fd => fd.Total);

                MessageBoxService.ShowMessage("Возврат по данной продаже уже был произведен ранее.",
                    "Возврат невозможен",
                    MessageButton.OK,
                    MessageIcon.Warning);
                return;
            }

            if (SelectedFuelSale.Tank == null)
            {
                MessageBoxService.ShowMessage(
                                "Произошла ошибка. Повторите попытку позже.",
                                "Ошибка системы",
                                MessageButton.OK,
                                MessageIcon.Error);
                return;
            }

            ShowLoadingPanel = true;

            try
            {
                var fuelSale = SelectedFuelSale.Clone();

                fuelSale.Sum = fuelSale.ReceivedSum;
                fuelSale.Quantity = fuelSale.ReceivedQuantity;

                //var fiscalData = await _cashRegisterStore.ReturnAsync(fuelSale, SelectedFuelSale.Tank.Fuel);

                //if (fiscalData != null)
                //{
                //    await _fiscalDataService.UpdateAsync(fiscalData.Id, fiscalData);
                //}
            }
            finally
            {
                ShowLoadingPanel = false;
            }
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            FuelSales = new(await _fuelSaleService.GetCompletedFuelSaleAsync(_shiftStore.CurrentShift.Id));
        }

        public async Task StartAsync()
        {
            await GetData();
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
