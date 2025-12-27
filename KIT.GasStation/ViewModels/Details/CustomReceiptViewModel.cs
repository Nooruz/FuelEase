using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class CustomReceiptViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<CustomReceiptViewModel> _logger;
        private readonly IFuelService _fuelService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private ObservableCollection<Fuel> _fuels = new();
        private Fuel _selectedFuel;
        private decimal _price;
        private decimal _quantity;
        private decimal _sum;
        private bool _isUpdatingQuantity;
        private bool _isUpdatingSum;
        private FuelSale _createdFuelSale = new() { PaymentType = PaymentType.Cash };

        #endregion

        #region Public Properties

        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public ObservableCollection<Fuel> Fuels
        {
            get => _fuels;
            set
            {
                _fuels = value;
                OnPropertyChanged(nameof(Fuels));
            }
        }
        public Fuel SelectedFuel
        {
            get => _selectedFuel;
            set
            {
                _selectedFuel = value;
                OnPropertyChanged(nameof(SelectedFuel));
                Price = SelectedFuel.Price;
            }
        }
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
                Sum = Math.Round(Price * (decimal)Quantity, 2);
            }
        }
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                if (!_isUpdatingSum)
                {
                    _isUpdatingQuantity = true;
                    Sum = Math.Round((decimal)Quantity * Price, 4);
                    _isUpdatingQuantity = false;
                }
            }
        }
        public decimal Sum
        {
            get => _sum;
            set
            {
                _sum = value;
                OnPropertyChanged(nameof(Sum));
                if (!_isUpdatingQuantity && Price != 0)
                {
                    _isUpdatingSum = true;
                    Quantity = Math.Round((Sum / Price), 6);
                    _isUpdatingSum = false;
                }
            }
        }
        public FuelSale CreatedFuelSale
        {
            get => _createdFuelSale;
            set
            {
                _createdFuelSale = value;
                OnPropertyChanged(nameof(CreatedFuelSale));
            }
        }

        #endregion

        #region Constructor

        public CustomReceiptViewModel(ILogger<CustomReceiptViewModel> logger,
            IFuelService fuelService,
            ICashRegisterStore cashRegisterStore)
        {
            _logger = logger;
            _fuelService = fuelService;
            _cashRegisterStore = cashRegisterStore;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Sale()
        {
            try
            {
                //await _cashRegisterStore.CustomReceiptAsync(new FuelSale
                //{
                //    Quantity = Quantity,
                //    Price = Price,
                //    Sum = Sum,
                //    CreateDate = DateTime.Now,
                //    Tank = new Tank
                //    {
                //        Fuel = SelectedFuel
                //    }
                //});
                CurrentWindowService.Close();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task StartAsync()
        {
            await OnStarted();
        }

        #endregion

        #region Private Voids

        private async Task OnStarted()
        {
            try
            {
                Fuels = new(await _fuelService.GetAllByUnitOfMeasurementAsync());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
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
