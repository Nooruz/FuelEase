using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class CompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private ObservableCollection<FuelSale> _selectedFuelSales = new();
        private FuelSale _selectedFuelSale;

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
        public FuelSale SelectedFuelSale
        {
            get => _selectedFuelSale;
            set
            {
                _selectedFuelSale = value;
                OnPropertyChanged(nameof(SelectedFuelSale));
                SelectedFuelSales.Clear();
                SelectedFuelSales.Add(SelectedFuelSale);
            }
        }

        #endregion

        #region Constructor

        public CompletedSalesViewModel(IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore)
        {
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
        }

        #endregion

        #region Public Voids

        [Command]
        public async void Return()
        {
            if (SelectedFuelSale == null)
            {
                MessageBoxService.ShowMessage("Для возврата необходимо сначала выбрать продажу.", 
                    "Выбор продажи",
                    MessageButton.OK,
                    MessageIcon.Warning);
                return;
            }

            if (SelectedFuelSale.FiscalData == null)
            {
                MessageBoxService.ShowMessage("Возврат через ККМ невозможен, так как у выбранной продажи отсутствуют фискальные данные.", 
                    "Ошибка возврата",
                    MessageButton.OK,
                    MessageIcon.Error);
                return;
            }

            if (SelectedFuelSale.FiscalData.ReturnCheck != null)
            {
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

            await _cashRegisterStore.ReturnAsync(SelectedFuelSale, SelectedFuelSale.Tank.Fuel);
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
