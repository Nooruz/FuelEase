using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FuelEase.ViewModels.Details
{
    public class CustomReceiptViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<CustomReceiptViewModel> _logger;
        private readonly IFuelService _fuelService;
        private ObservableCollection<Fuel> _fuels = new();
        private Fuel _selectedFuel;
        private decimal _price;
        private double _quantity;
        private decimal _sum;
        private bool _isUpdatingQuantity;
        private bool _isUpdatingSum;

        #endregion

        #region Public Properties

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
        public double Quantity
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
                    Quantity = Math.Round((double)(Sum / Price), 6);
                    _isUpdatingSum = false;
                }
            }
        }

        #endregion

        #region Constructor

        public CustomReceiptViewModel(ILogger<CustomReceiptViewModel> logger,
            IFuelService fuelService)
        {
            _logger = logger;
            _fuelService = fuelService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Sale()
        {
            try
            {
                //await _cashRegisterManager.CustomReceiptAsync(new FuelSale
                //{
                //    PaymentType = PaymentType.Arbitrary,
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
