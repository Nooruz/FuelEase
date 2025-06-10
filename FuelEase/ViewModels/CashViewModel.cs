using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.State.Shifts;
using FuelEase.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;

namespace FuelEase.ViewModels
{
    public class CashViewModel : PanelViewModel
    {
        #region Private Members

        private string _shiftStatus = string.Empty;
        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private decimal _cash;
        private decimal _cashless;
        private decimal _arbitrary;

        #endregion

        #region Public Properties

        public ObservableCollection<FuelSale> FuelSales => _fuelSales;
        public string ShiftStatus => GetShiftStatus();
        public decimal Cash => FuelSales
                    .Where(f => f.PaymentType == PaymentType.Cash)
                    .Sum(f => f.ReceivedSum);
        public decimal Cashless => FuelSales
                    .Where(f => f.PaymentType == PaymentType.Cashless)
                    .Sum(f => f.ReceivedSum);
        public Shift CurrentShift => _shiftStore.CurrentShift;

        #endregion

        #region Constructors

        public CashViewModel(IShiftStore shiftStore,
            IFuelSaleService fuelSaleService)
        {
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;

            _shiftStore.OnClosed += shift =>  OnShiftUpdated(shift);
            _shiftStore.OnOpened += shift => OnShiftUpdated(shift);
            _shiftStore.OnLogin += shift => OnShiftUpdated(shift);
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
        }

        #endregion

        #region Private Voids

        private void OnShiftUpdated(Shift shift)
        {
            _ = App.Current.Dispatcher.Invoke(async () =>
            {
                FuelSales.Clear();

                if (shift != null)
                {
                    var fuelSales = await _fuelSaleService.GetAllAsync(shift.Id);
                    foreach (var sale in fuelSales)
                    {
                        FuelSales.Add(sale);
                    }
                }
                UpdateProperties();
            });
        }

        private void FuelSaleService_OnUpdated(FuelSale updatedFuelSale)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                // Найти существующий элемент
                var existingFuelSale = FuelSales.FirstOrDefault(fs => fs.Id == updatedFuelSale.Id);

                if (existingFuelSale != null)
                {
                    // Заменить существующий элемент
                    int index = FuelSales.IndexOf(existingFuelSale);
                    FuelSales[index] = updatedFuelSale;
                }
                else
                {
                    // Добавить новый элемент
                    FuelSales.Add(updatedFuelSale);
                }

                // Обновить свойства
                UpdateProperties();
            });
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(ShiftStatus));
            OnPropertyChanged(nameof(Cash));
            OnPropertyChanged(nameof(Cashless));
            OnPropertyChanged(nameof(CurrentShift));
        }

        private string GetShiftStatus()
        {
            if (CurrentShift == null)
            {
                return "Откройте смену.";
            }

            return CurrentShift.ShiftState switch
            {
                ShiftState.None => string.Empty,
                ShiftState.Open => $"Смена №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (24 часа не прошли)",
                ShiftState.Closed => $"Смена №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (смена закрыта {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm})",
                ShiftState.Exceeded24Hours => $"Смена №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (прошло более 24 часов)",
                _ => string.Empty,
            };
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _shiftStore.OnClosed -= shift => OnShiftUpdated(shift);
                _shiftStore.OnOpened -= shift => OnShiftUpdated(shift);
                _shiftStore.OnLogin -= shift => OnShiftUpdated(shift);
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
