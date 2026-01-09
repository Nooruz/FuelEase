using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;

namespace KIT.GasStation.ViewModels
{
    public class CashViewModel : PanelViewModel
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private string _cashRegisterShiftStatus = string.Empty;

        #endregion

        #region Public Properties

        public ObservableCollection<FuelSale> FuelSales => _fuelSales;
        public string ShiftStatus => GetShiftStatus();
        public string CashRegisterShiftStatus
        {
            get => _cashRegisterShiftStatus;
            set
            {
                _cashRegisterShiftStatus = value;
                OnPropertyChanged(nameof(CashRegisterShiftStatus));
            }
        }
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
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore)
        {
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;

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

                switch (_cashRegisterStore.Status)
                {
                    case CashRegisterStatus.Unknown:
                        CashRegisterShiftStatus = "Смена ККМ: неизвестен статус";
                        break;
                    case CashRegisterStatus.Open:
                        CashRegisterShiftStatus = $"Смена ККМ: открыта {_cashRegisterStore.OpenAt:dd.MM.yyyy HH:mm}";
                        break;
                    case CashRegisterStatus.Close:
                        CashRegisterShiftStatus = $"Смена ККМ: открыта {_cashRegisterStore.OpenAt:dd.MM.yyyy HH:mm}";
                        break;
                    case CashRegisterStatus.Exceeded24Hours:
                        CashRegisterShiftStatus = $"Смена ККМ: превышено 24 часа с момента открытия {_cashRegisterStore.OpenAt:dd.MM.yyyy HH:mm}";
                        break;
                    default:
                        break;
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
                return "Смена СУ: Откройте смену.";
            }

            return CurrentShift.ShiftState switch
            {
                ShiftState.None => "Смена СУ: Откройте смену.",
                ShiftState.Open => $"Смена СУ: №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (24 часа не прошли)",
                ShiftState.Closed => $"Смена СУ: №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (смена закрыта {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm})",
                ShiftState.Exceeded24Hours => $"Смена СУ: №{CurrentShift.Id} от {CurrentShift.OpeningDate:dd.MM.yyyy HH:mm} (прошло более 24 часов)",
                _ => "Смена СУ: Откройте смену.",
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
